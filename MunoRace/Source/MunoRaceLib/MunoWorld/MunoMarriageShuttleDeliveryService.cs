using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责创建载有和亲公主的缪诺穿梭机并安排其入场。
    /// </summary>
    public static class MunoMarriageShuttleDeliveryService
    {
        private const float LandingSearchRadius = 10f;

        /// <summary>
        /// 创建并生成载有和亲公主的入场穿梭机。
        /// </summary>
        public static bool TrySpawnPrincessShuttle(Pawn negotiator, Pawn princess, out Thing shuttle, out string failReason)
        {
            return TrySpawnPrincessShuttle(negotiator.Map, negotiator.Position, princess, null, out shuttle, out failReason);
        }

        /// <summary>
        /// 创建并生成载有和亲公主与嫁妆的入场穿梭机。
        /// </summary>
        public static bool TrySpawnPrincessShuttle(Map map, IntVec3 near, Pawn princess, List<Thing> dowry, out Thing shuttle, out string failReason)
        {
            shuttle = null;
            failReason = null;
            if (map == null)
            {
                failReason = "没有可用于接收和亲穿梭机的地图。";
                return false;
            }

            if (!TryFindLandingCell(map, near, out IntVec3 landingCell))
            {
                failReason = "附近没有可供和亲穿梭机降落的安全区域。";
                return false;
            }

            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            shuttle = QuestGen_Shuttle.GenerateShuttle(munoFaction, null, null, acceptColonists: false, onlyAcceptColonists: false, onlyAcceptHealthy: false, 0, dropEverythingIfUnsatisfied: false, leaveImmediatelyWhenSatisfied: false, dropEverythingOnArrival: false, stayAfterDroppedEverythingOnArrival: true, map.Parent, map.Parent, -1, ThingDefOf.Shuttle, permitShuttle: false, hideControls: true, allowSlaves: false, requireAllColonistsOnMap: false, acceptColonyPrisoners: false);
            if (shuttle == null)
            {
                failReason = "未能生成缪诺和亲穿梭机。";
                return false;
            }

            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            CompShuttle shuttleComp = shuttle.TryGetComp<CompShuttle>();
            if (transporter == null || shuttleComp == null)
            {
                shuttle.Destroy();
                failReason = "缪诺和亲穿梭机缺少必要组件。";
                return false;
            }

            if (dowry != null)
            {
                for (int i = dowry.Count - 1; i >= 0; i--)
                {
                    Thing thing = dowry[i];
                    if (thing != null && !thing.Destroyed)
                    {
                        if (!transporter.innerContainer.TryAdd(thing, canMergeWithExistingStacks: true))
                        {
                            failReason = "未能把全部和亲嫁妆载入穿梭机。";
                            shuttle.Destroy(DestroyMode.Vanish);
                            return false;
                        }
                    }
                }
            }

            if (Find.WorldPawns.Contains(princess))
            {
                Find.WorldPawns.RemovePawn(princess);
            }

            if (!transporter.innerContainer.TryAdd(princess))
            {
                if (!Find.WorldPawns.Contains(princess))
                {
                    Find.WorldPawns.PassToWorld(princess, PawnDiscardDecideMode.KeepForever);
                }

                failReason = "未能把和亲公主载入穿梭机。";
                shuttle.Destroy(DestroyMode.Vanish);
                return false;
            }

            TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle);
            shuttleComp.shipParent = transportShip;
            transportShip.AddJobs(ShipJobDefOf.WaitForever, ShipJobDefOf.FlyAway);

            Thing skyfaller = SkyfallerMaker.MakeSkyfaller(ThingDefOf.ShuttleIncoming, shuttle);
            if (!GenPlace.TryPlaceThing(skyfaller, landingCell, map, ThingPlaceMode.Near))
            {
                failReason = "未能把缪诺和亲穿梭机放入降落区域。";
                shuttle.Destroy(DestroyMode.Vanish);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 在谈判者附近寻找穿梭机降落点。
        /// </summary>
        private static bool TryFindLandingCell(Map map, IntVec3 near, out IntVec3 landingCell)
        {
            return CellFinder.TryFindRandomCellNear(near, map, (int)LandingSearchRadius, cell => RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(cell, map).Accepted, out landingCell);
        }
    }
}
