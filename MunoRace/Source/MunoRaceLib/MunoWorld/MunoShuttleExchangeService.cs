using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责创建批量人口接收穿梭机，并将目标与锁定奖励绑定到交换会话。
    public static class MunoShuttleExchangeService
    {
        private const float LandingSearchRadius = 10f;

        //启动一轮批量缪诺穿梭机交换流程。
        public static bool TryStartExchange(Pawn negotiator, List<MunoExchangeTargetRecord> targets, List<Thing> itemRewards, out string failReason)
        {
            failReason = null;
            bool ownsGeneratedItems = false;
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

            if (!TryBuildTargetLists(targets, negotiator.Map, out List<MunoExchangeTargetRecord> sessionTargets, out List<Pawn> targetPawns, out failReason))
            {
                return false;
            }

            if (itemRewards == null)
            {
                if (!MunoExchangeRewardService.TryGenerateRandomItemReward(sessionTargets, out itemRewards, out _, out failReason))
                {
                    return false;
                }

                ownsGeneratedItems = true;
            }

            if (!ValidateItemRewards(itemRewards, out failReason))
            {
                if (ownsGeneratedItems)
                {
                    MunoExchangeRewardService.DestroyItems(itemRewards);
                }
                return false;
            }

            MunoShuttleExchangeSession session = CurrentSession();
            if (session.HasActiveSession)
            {
                failReason = "已有一架缪诺接收穿梭机正在执行任务，请等待当前流程结束。";
                if (ownsGeneratedItems)
                {
                    MunoExchangeRewardService.DestroyItems(itemRewards);
                }
                return false;
            }

            if (!TryFindLandingCell(negotiator.Map, negotiator.Position, out IntVec3 landingCell))
            {
                failReason = "附近没有可供缪诺穿梭机降落的安全区域。";
                if (ownsGeneratedItems)
                {
                    MunoExchangeRewardService.DestroyItems(itemRewards);
                }
                return false;
            }

            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            if (munoFaction == null)
            {
                failReason = "未找到有效的缪诺派系实例。";
                if (ownsGeneratedItems)
                {
                    MunoExchangeRewardService.DestroyItems(itemRewards);
                }
                return false;
            }

            Thing shuttle = QuestGen_Shuttle.GenerateShuttle(
                owningFaction: munoFaction,
                requiredPawns: targetPawns,
                requiredItems: null,
                acceptColonists: false,
                onlyAcceptColonists: false,
                onlyAcceptHealthy: false,
                requireColonistCount: 0,
                dropEverythingIfUnsatisfied: true,
                leaveImmediatelyWhenSatisfied: true,
                dropEverythingOnArrival: false,
                stayAfterDroppedEverythingOnArrival: true,
                missionShuttleTarget: negotiator.Map.Parent,
                missionShuttleHome: negotiator.Map.Parent,
                maxColonistCount: -1,
                shuttleDef: ThingDefOf.Shuttle,
                permitShuttle: false,
                hideControls: true,
                allowSlaves: false,
                requireAllColonistsOnMap: false,
                acceptColonyPrisoners: false);

            if (shuttle == null)
            {
                failReason = "未能生成缪诺接收穿梭机。";
                if (ownsGeneratedItems)
                {
                    MunoExchangeRewardService.DestroyItems(itemRewards);
                }
                return false;
            }

            CompShuttle shuttleComp = shuttle.TryGetComp<CompShuttle>();
            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            if (shuttleComp == null || transporter == null)
            {
                shuttle.Destroy();
                failReason = "缪诺接收穿梭机缺少必要组件，无法执行流程。";
                if (ownsGeneratedItems)
                {
                    MunoExchangeRewardService.DestroyItems(itemRewards);
                }
                return false;
            }

            ConfigureShuttle(shuttleComp, targetPawns);
            ConfigureLoading(transporter, targetPawns);
            TransportShip transportShip = ConfigureTransportShip(shuttle, shuttleComp, targetPawns);

            if (!TrySpawnIncomingShuttle(shuttle, negotiator.Map, landingCell, out failReason))
            {
                transportShip.EndCurrentJob();
                if (!shuttle.Destroyed)
                {
                    shuttle.Destroy(DestroyMode.Vanish);
                }
                if (ownsGeneratedItems)
                {
                    MunoExchangeRewardService.DestroyItems(itemRewards);
                }
                return false;
            }

            session.StartSession(negotiator, sessionTargets, itemRewards, shuttle, negotiator.Map);
            return true;
        }

        //返回当前存档中的缪诺穿梭机交换会话组件。
        public static MunoShuttleExchangeSession CurrentSession()
        {
            return Current.Game.GetComponent<MunoShuttleExchangeSession>();
        }

        //复制并验证目标记录，同时构建原版穿梭机需要的 Pawn 列表。
        private static bool TryBuildTargetLists(List<MunoExchangeTargetRecord> targets, Map map, out List<MunoExchangeTargetRecord> sessionTargets, out List<Pawn> targetPawns, out string failReason)
        {
            sessionTargets = new List<MunoExchangeTargetRecord>();
            targetPawns = new List<Pawn>();
            failReason = null;
            if (targets == null || targets.Count == 0)
            {
                failReason = "至少选择一名接收目标。";
                return false;
            }

            HashSet<Pawn> seen = new HashSet<Pawn>();
            for (int i = 0; i < targets.Count; i++)
            {
                MunoExchangeTargetRecord target = targets[i];
                if (target?.pawn == null || !seen.Add(target.pawn))
                {
                    failReason = "接收目标列表包含无效或重复人员。";
                    return false;
                }

                if (target.rewardType != MunoExchangeRewardType.MunoPawn && target.rewardType != MunoExchangeRewardType.RandomItems)
                {
                    failReason = "交换奖励类型无效。";
                    return false;
                }

                if (!MunoHostageExchangeService.IsEligibleCandidateOnMap(target.pawn, map))
                {
                    failReason = "所选目标已不再符合缪诺接收条件。";
                    return false;
                }

                sessionTargets.Add(new MunoExchangeTargetRecord(target.pawn, target.rewardType));
                targetPawns.Add(target.pawn);
            }

            return true;
        }

        //校验已生成的随机物资是否仍可交付。
        private static bool ValidateItemRewards(List<Thing> itemRewards, out string failReason)
        {
            failReason = null;
            if (itemRewards == null)
            {
                failReason = "随机物资奖励列表无效。";
                return false;
            }

            for (int i = 0; i < itemRewards.Count; i++)
            {
                if (itemRewards[i] == null || itemRewards[i].Destroyed || itemRewards[i].ParentHolder != null)
                {
                    failReason = "随机物资奖励已失效，请重新生成奖励预览。";
                    return false;
                }
            }

            return true;
        }

        //配置穿梭机只接受本次选择的全部目标。
        private static void ConfigureShuttle(CompShuttle shuttleComp, List<Pawn> targetPawns)
        {
            shuttleComp.requiredPawns.Clear();
            shuttleComp.requiredPawns.AddRange(targetPawns);
            shuttleComp.acceptColonists = false;
            shuttleComp.onlyAcceptColonists = false;
            shuttleComp.allowSlaves = false;
            shuttleComp.acceptColonyPrisoners = false;
            shuttleComp.requiredColonistCount = 0;
            shuttleComp.maxColonistCount = -1;
        }

        //初始化运输组件并将全部目标加入装载清单。
        private static void ConfigureLoading(CompTransporter transporter, List<Pawn> targetPawns)
        {
            TransporterUtility.InitiateLoading(Gen.YieldSingle(transporter));
            for (int i = 0; i < targetPawns.Count; i++)
            {
                TransferableOneWay transferable = new TransferableOneWay();
                transferable.things.Add(targetPawns[i]);
                transporter.AddToTheToLoadList(transferable, 1);
            }
        }

        //创建等待全部目标完成装载的运输船任务。
        private static TransportShip ConfigureTransportShip(Thing shuttle, CompShuttle shuttleComp, List<Pawn> targetPawns)
        {
            TransportShip transportShip = shuttleComp.shipParent ?? TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle);
            ShipJob_WaitForever waitJob = (ShipJob_WaitForever)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitForever);
            waitJob.leaveImmediatelyWhenSatisfied = false;
            waitJob.showGizmos = false;
            transportShip.ForceJob(waitJob);
            return transportShip;
        }

        //优先采用玩家着陆信标区域，没有可用信标时再在谈判者附近寻找安全位置。
        private static bool TryFindLandingCell(Map map, IntVec3 near, out IntVec3 landingCell)
        {
            if (DropCellFinder.TryFindShipLandingArea(map, out landingCell, out _))
            {
                return true;
            }

            return CellFinder.TryFindRandomCellNear(near, map, (int)LandingSearchRadius, cell => RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(cell, map).Accepted, out landingCell);
        }

        //以原版穿梭机入场天降物形式将接收穿梭机放入地图。
        private static bool TrySpawnIncomingShuttle(Thing shuttle, Map map, IntVec3 landingCell, out string failReason)
        {
            failReason = null;
            if (shuttle == null || map == null)
            {
                failReason = "穿梭机或地图无效，无法执行缪诺接收流程。";
                return false;
            }

            Thing skyfaller = SkyfallerMaker.MakeSkyfaller(ThingDefOf.ShuttleIncoming, shuttle);
            if (skyfaller == null)
            {
                failReason = "未能生成缪诺接收穿梭机的降落天降物。";
                return false;
            }

            if (!GenPlace.TryPlaceThing(skyfaller, landingCell, map, ThingPlaceMode.Near))
            {
                failReason = "未能把缪诺接收穿梭机放入目标降落区域。";
                return false;
            }

            return true;
        }
    }
}
