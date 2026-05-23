using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责发起缪诺穿梭机接收流程，并把选中目标绑定到真实穿梭机任务中。
    /// </summary>
    public static class MunoShuttleExchangeService
    {
        private const float LandingSearchRadius = 10f;

        /// <summary>
        /// 启动一轮缪诺穿梭机交换流程，并在当前地图生成只接收指定目标的穿梭机。
        /// </summary>
        public static bool TryStartExchange(Pawn negotiator, Pawn targetPawn, out string failReason)
        {
            failReason = null;
            if (negotiator?.Map == null)
            {
                failReason = "当前谈判者不在有效地图中，无法请求缪诺接收穿梭机。";
                return false;
            }

            if (MunoDefDataRef.MunoColony_Faction == null)
            {
                failReason = "缪诺派系定义缺失，无法请求缪诺接收穿梭机。";
                return false;
            }

            if (!MunoHostageExchangeService.IsEligibleCandidateOnMap(targetPawn, negotiator.Map))
            {
                failReason = "所选目标已不再符合缪诺接收条件。";
                return false;
            }

            MunoShuttleExchangeSession session = CurrentSession();
            if (session.HasActiveSession)
            {
                failReason = "已有一架缪诺接收穿梭机正在执行任务，请等待当前流程结束。";
                return false;
            }

            if (!TryFindLandingCell(negotiator.Map, negotiator.Position, out IntVec3 landingCell))
            {
                failReason = "附近没有可供缪诺穿梭机降落的安全区域。";
                return false;
            }

            Thing shuttle = QuestGen_Shuttle.GenerateShuttle(
                owningFaction: Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction),
                requiredPawns: Gen.YieldSingle(targetPawn),
                requiredItems: null,
                acceptColonists: true,
                onlyAcceptColonists: false,
                onlyAcceptHealthy: false,
                requireColonistCount: 0,
                dropEverythingIfUnsatisfied: true,
                leaveImmediatelyWhenSatisfied: true,
                dropEverythingOnArrival: false,
                stayAfterDroppedEverythingOnArrival: true,
                missionShuttleTarget: negotiator.Map.Parent,
                missionShuttleHome: negotiator.Map.Parent,
                maxColonistCount: 0,
                shuttleDef: ThingDefOf.Shuttle,
                permitShuttle: false,
                hideControls: true,
                allowSlaves: true,
                requireAllColonistsOnMap: false,
                acceptColonyPrisoners: true);

            if (shuttle == null)
            {
                failReason = "未能生成缪诺接收穿梭机。";
                return false;
            }

            CompShuttle shuttleComp = shuttle.TryGetComp<CompShuttle>();
            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            if (shuttleComp == null || transporter == null)
            {
                shuttle.Destroy();
                failReason = "缪诺接收穿梭机缺少必要组件，无法执行流程。";
                return false;
            }

            shuttleComp.requiredPawns.Clear();
            shuttleComp.requiredPawns.Add(targetPawn);
            shuttleComp.acceptColonists = true;
            shuttleComp.allowSlaves = true;
            shuttleComp.acceptColonyPrisoners = true;
            shuttleComp.requiredColonistCount = 0;
            shuttleComp.maxColonistCount = 0;

            TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle);
            transportShip.ArriveAt(landingCell, negotiator.Map.Parent);
            ShipJob_WaitForever waitJob = (ShipJob_WaitForever)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitForever);
            waitJob.leaveImmediatelyWhenSatisfied = true;
            waitJob.showGizmos = false;
            waitJob.sendAwayIfAnyDespawnedDownedOrDead = new System.Collections.Generic.List<Thing> { targetPawn };
            transportShip.AddJob(waitJob);
            transportShip.AddJob(ShipJobDefOf.FlyAway);

            CurrentSession().StartSession(negotiator, targetPawn, shuttle, negotiator.Map);
            return true;
        }

        /// <summary>
        /// 返回当前存档中的缪诺穿梭机交换会话组件。
        /// </summary>
        public static MunoShuttleExchangeSession CurrentSession()
        {
            return Current.Game.GetComponent<MunoShuttleExchangeSession>();
        }

        /// <summary>
        /// 在谈判者附近寻找一块可供原版穿梭机安全降落的位置。
        /// </summary>
        private static bool TryFindLandingCell(Map map, IntVec3 near, out IntVec3 landingCell)
        {
            return CellFinder.TryFindRandomCellNear(near, map, (int)LandingSearchRadius, cell => RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(cell, map).Accepted, out landingCell);
        }
    }
}
