using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责处理缪诺人口交换的候选筛选、批量校验和远行队事务。
    public static class MunoHostageExchangeService
    {
        //判断指定据点是否属于缪诺派系。
        public static bool IsMunoSettlement(Settlement settlement)
        {
            return settlement?.Faction?.def == MunoDefDataRef.MunoColony_Faction;
        }

        //判断远行队是否正停驻在可执行交换的缪诺据点。
        public static bool CanExchangeAt(Settlement settlement, Caravan caravan)
        {
            if (settlement == null || caravan == null || !caravan.IsPlayerControlled)
            {
                return false;
            }

            return IsMunoSettlement(settlement) && CaravanVisitUtility.SettlementVisitedNow(caravan) == settlement;
        }

        //收集远行队内所有符合缪诺交换条件的人形成人。
        public static List<Pawn> GetExchangeCandidates(Caravan caravan)
        {
            List<Pawn> result = new List<Pawn>();
            if (caravan == null)
            {
                return result;
            }

            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (IsEligibleCandidate(pawns[i]))
                {
                    result.Add(pawns[i]);
                }
            }

            return result;
        }

        //收集地图内所有符合缪诺接收条件的人形成人。
        public static List<Pawn> GetExchangeCandidates(Map map)
        {
            List<Pawn> result = new List<Pawn>();
            if (map == null)
            {
                return result;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (IsEligibleCandidateOnMap(pawns[i], map))
                {
                    result.Add(pawns[i]);
                }
            }

            return result;
        }

        //判断 Pawn 是否符合交换的基础生物和身份条件。
        public static bool IsEligibleCandidate(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            if (!pawn.RaceProps.Humanlike || pawn.DevelopmentalStage.Baby())
            {
                return false;
            }

            return IsEligibleColonist(pawn) || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony;
        }

        //判断 Pawn 是否仍位于指定地图并符合交换条件。
        public static bool IsEligibleCandidateOnMap(Pawn pawn, Map map)
        {
            return IsEligibleCandidate(pawn) && pawn.Map == map;
        }

        //返回候选 Pawn 的身份文本。
        public static string GetPawnRoleLabel(Pawn pawn)
        {
            if (pawn == null)
            {
                return "未知";
            }

            if (pawn.IsPrisonerOfColony)
            {
                return "囚犯";
            }

            if (pawn.IsSlaveOfColony)
            {
                return "奴隶";
            }

            if (IsEligibleColonist(pawn))
            {
                return "殖民者";
            }

            return "其他";
        }

        //返回候选 Pawn 的当前健康或精神状态文本。
        public static string GetPawnStatusLabel(Pawn pawn)
        {
            if (pawn == null)
            {
                return "未知";
            }

            if (pawn.Downed)
            {
                return "倒地";
            }

            if (pawn.InMentalState)
            {
                return pawn.MentalStateDef?.label ?? "精神异常";
            }

            if (pawn.health?.summaryHealth != null)
            {
                return pawn.health.summaryHealth.SummaryHealthPercent.ToStringPercent();
            }

            return "稳定";
        }

        //批量完成据点交换，并按每名目标的奖励选择发放奖励。
        public static bool TryExchangePawns(Settlement settlement, Caravan caravan, List<MunoExchangeTargetRecord> targets, List<Thing> itemRewards, out string failReason, out List<Pawn> joinedPawns)
        {
            failReason = null;
            joinedPawns = new List<Pawn>();
            if (!CanExchangeAt(settlement, caravan))
            {
                failReason = "当前远行队未停驻在可交换的缪诺据点。";
                return false;
            }

            if (!TryValidateTargetRecords(targets, null, caravan, out failReason))
            {
                return false;
            }

            if (MunoDefDataRef.MunoColony_Faction == null)
            {
                failReason = "缪诺据点派系数据无效。";
                return false;
            }

            if (itemRewards == null)
            {
                if (!MunoExchangeRewardService.TryGenerateRandomItemReward(targets, out itemRewards, out _, out failReason))
                {
                    return false;
                }
            }

            int munoRewardCount = CountRewardType(targets, MunoExchangeRewardType.MunoPawn);
            if (!MunoExchangeRewardService.TryGenerateMunoPawns(munoRewardCount, out List<Pawn> generatedPawns, out failReason))
            {
                MunoExchangeRewardService.DestroyItems(itemRewards);
                return false;
            }

            List<Pawn> offeredPawns = new List<Pawn>();
            for (int i = 0; i < targets.Count; i++)
            {
                offeredPawns.Add(targets[i].pawn);
            }

            if (!MunoExchangeRewardService.TryDeliverRewardsToCaravan(caravan, generatedPawns, itemRewards, out failReason, offeredPawns))
            {
                MunoExchangeRewardService.DestroyPawns(generatedPawns);
                MunoExchangeRewardService.DestroyItems(itemRewards);
                return false;
            }

            Faction munoFaction = settlement.Faction;
            for (int i = 0; i < targets.Count; i++)
            {
                TransferPawnToMunoFaction(caravan, munoFaction, targets[i].pawn);
            }

            joinedPawns.AddRange(generatedPawns);
            return true;
        }

        //判断 Pawn 是否属于玩家可控制的殖民者，并排除自由缪诺殖民者。
        private static bool IsEligibleColonist(Pawn pawn)
        {
            return !IsMunoPawn(pawn)
                && !pawn.IsQuestLodger()
                && (pawn.IsColonistPlayerControlled || (pawn.IsColonist && pawn.IsCaravanMember()));
        }

        //判断 Pawn 是否属于缪诺种族，避免自由缪诺成员被再次交换。
        private static bool IsMunoPawn(Pawn pawn)
        {
            if (pawn == null || MunoDefDataRef.MunoRace_Colonist?.race == null)
            {
                return false;
            }

            return pawn.def == MunoDefDataRef.MunoRace_Colonist.race;
        }

        //验证批量目标的唯一性、地图或远行队归属及当前交换资格。
        private static bool TryValidateTargetRecords(List<MunoExchangeTargetRecord> targets, Map map, Caravan caravan, out string failReason)
        {
            failReason = null;
            if (targets == null || targets.Count == 0)
            {
                failReason = "至少选择一名交换目标。";
                return false;
            }

            HashSet<Pawn> seen = new HashSet<Pawn>();
            for (int i = 0; i < targets.Count; i++)
            {
                MunoExchangeTargetRecord target = targets[i];
                if (target?.pawn == null || !seen.Add(target.pawn))
                {
                    failReason = "交换目标列表包含无效或重复人员。";
                    return false;
                }

                if (target.rewardType != MunoExchangeRewardType.MunoPawn && target.rewardType != MunoExchangeRewardType.RandomItems)
                {
                    failReason = "交换奖励类型无效。";
                    return false;
                }

                if (!IsEligibleCandidate(target.pawn))
                {
                    failReason = "所选目标已不再符合缪诺交换条件。";
                    return false;
                }

                if (map != null && !IsEligibleCandidateOnMap(target.pawn, map))
                {
                    failReason = "所选目标已离开当前地图。";
                    return false;
                }

                if (caravan != null && !caravan.ContainsPawn(target.pawn))
                {
                    failReason = "所选目标已离开当前远行队。";
                    return false;
                }
            }

            return true;
        }

        //统计指定奖励类型的目标数量。
        private static int CountRewardType(List<MunoExchangeTargetRecord> targets, MunoExchangeRewardType rewardType)
        {
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].rewardType == rewardType)
                {
                    count++;
                }
            }

            return count;
        }

        //将上交 Pawn 移出远行队并交给缪诺派系保留为世界 Pawn。
        private static void TransferPawnToMunoFaction(Caravan caravan, Faction munoFaction, Pawn pawn)
        {
            caravan.RemovePawn(pawn);
            caravan.Notify_PawnRemoved(pawn);
            pawn.DeSpawnOrDeselect();
            pawn.guest?.SetGuestStatus(null);
            pawn.SetFaction(munoFaction);
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }
        }
    }
}
