using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责处理缪诺据点的人质交换筛选、校验、移交与新成员加入远行队的事务逻辑。
    /// </summary>
    public static class MunoHostageExchangeService
    {
        /// <summary>
        /// 返回当前据点是否为可执行交换的缪诺据点。
        /// </summary>
        public static bool IsMunoSettlement(Settlement settlement)
        {
            return settlement?.Faction?.def == MunoDefDataRef.MunoColony_Faction;
        }

        /// <summary>
        /// 判断当前远行队是否正停驻在目标缪诺据点。
        /// </summary>
        public static bool CanExchangeAt(Settlement settlement, Caravan caravan)
        {
            if (settlement == null || caravan == null || !caravan.IsPlayerControlled)
            {
                return false;
            }

            if (!IsMunoSettlement(settlement))
            {
                return false;
            }

            return CaravanVisitUtility.SettlementVisitedNow(caravan) == settlement;
        }

        /// <summary>
        /// 收集当前远行队内所有允许上交给缪诺据点的目标 Pawn。
        /// </summary>
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
                Pawn pawn = pawns[i];
                if (IsEligibleCandidate(pawn))
                {
                    result.Add(pawn);
                }
            }

            return result;
        }

        /// <summary>
        /// 收集当前地图内所有允许上交给缪诺接收穿梭机的目标 Pawn。
        /// </summary>
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
                Pawn pawn = pawns[i];
                if (IsEligibleCandidateOnMap(pawn, map))
                {
                    result.Add(pawn);
                }
            }

            return result;
        }

        /// <summary>
        /// 判断指定 Pawn 是否符合当前交换候选条件。
        /// </summary>
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

        /// <summary>
        /// 判断指定 Pawn 是否符合当前地图内缪诺接收流程的候选条件。
        /// </summary>
        public static bool IsEligibleCandidateOnMap(Pawn pawn, Map map)
        {
            if (!IsEligibleCandidate(pawn))
            {
                return false;
            }

            return pawn.Map == map;
        }

        /// <summary>
        /// 返回候选 Pawn 在列表中显示的身份文本。
        /// </summary>
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

        /// <summary>
        /// 返回候选 Pawn 的当前状态文本。
        /// </summary>
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

        /// <summary>
        /// 执行一次单人交换事务；成功时移交旧成员并让新的缪诺成员直接加入当前远行队。
        /// </summary>
        public static bool TryExchangePawn(Settlement settlement, Caravan caravan, Pawn offeredPawn, out string failReason, out Pawn joinedPawn)
        {
            failReason = null;
            joinedPawn = null;

            if (!CanExchangeAt(settlement, caravan))
            {
                failReason = "当前远行队未停驻在可交换的缪诺据点。";
                return false;
            }

            if (!IsEligibleCandidate(offeredPawn) || !caravan.ContainsPawn(offeredPawn))
            {
                failReason = "所选目标已不再符合交换条件。";
                return false;
            }

            if (settlement.Faction == null)
            {
                failReason = "缪诺据点派系数据无效。";
                return false;
            }

            if (MunoDefDataRef.MunoRace_Colonist == null)
            {
                failReason = "缪诺殖民者模板缺失，无法完成交换。";
                return false;
            }

            Pawn generatedPawn = null;
            try
            {
                if (!TryGenerateRewardPawn(out generatedPawn, out failReason))
                {
                    return false;
                }

                if (!TryAddRewardPawnToCaravan(caravan, generatedPawn, out failReason))
                {
                    return false;
                }

                TransferPawnToMunoFaction(caravan, settlement.Faction, offeredPawn);
                joinedPawn = generatedPawn;
                return true;
            }
            catch (System.Exception ex)
            {
                if (generatedPawn != null && caravan != null && caravan.ContainsPawn(generatedPawn))
                {
                    caravan.RemovePawn(generatedPawn);
                    caravan.Notify_PawnRemoved(generatedPawn);
                }

                if (generatedPawn != null && generatedPawn.ParentHolder == null && !generatedPawn.Destroyed)
                {
                    generatedPawn.Destroy();
                }

                Log.Error("缪诺据点交换成员时发生异常: " + ex);
                failReason = "交换过程中发生异常，未执行本次交换。";
                return false;
            }
        }

        /// <summary>
        /// 判断指定 Pawn 是否属于玩家可控制殖民者，且兼容远行队中的未生成成员。
        /// </summary>
        private static bool IsEligibleColonist(Pawn pawn)
        {
            return !IsMunoPawn(pawn) && (pawn.IsColonistPlayerControlled || (pawn.IsColonist && pawn.IsCaravanMember()));
        }

        /// <summary>
        /// 判断指定 Pawn 是否属于缪诺种族，用于避免把缪诺殖民者再次拿去交换缪诺。
        /// </summary>
        private static bool IsMunoPawn(Pawn pawn)
        {
            if (pawn == null || MunoDefDataRef.MunoRace_Colonist?.race == null)
            {
                return false;
            }

            return pawn.def == MunoDefDataRef.MunoRace_Colonist.race;
        }

        /// <summary>
        /// 生成一个可作为交换奖励发放的缪诺殖民者。
        /// </summary>
        public static bool TryGenerateRewardPawn(out Pawn pawn, out string failReason)
        {
            failReason = null;
            pawn = null;
            if (MunoDefDataRef.MunoRace_Colonist == null)
            {
                failReason = "缪诺殖民者模板缺失，无法生成奖励成员。";
                return false;
            }

            pawn = GenerateMunoColonist();
            if (pawn == null)
            {
                failReason = "未能生成新的缪诺成员。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 将奖励缪诺成员安全加入远行队，并校验其最终状态是否正确。
        /// </summary>
        public static bool TryAddRewardPawnToCaravan(Caravan caravan, Pawn pawn, out string failReason)
        {
            failReason = null;
            if (caravan == null || pawn == null)
            {
                failReason = "远行队或奖励成员无效，无法完成加入。";
                return false;
            }

            AddGeneratedPawnToCaravan(caravan, pawn);
            if (!IsJoinedPawnValid(pawn))
            {
                failReason = "新缪诺成员加入远行队后未能恢复为玩家殖民者状态。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 将奖励缪诺成员安全放入当前地图，并尝试在目标附近为其寻找可站立位置。
        /// </summary>
        public static bool TryAddRewardPawnToMap(Map map, Pawn pawn, out string failReason)
        {
            failReason = null;
            if (map == null || pawn == null)
            {
                failReason = "地图或奖励成员无效，无法完成加入。";
                return false;
            }

            NormalizeJoinedPawnState(pawn);
            IntVec3 dropCell;
            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(map.Center, map, out dropCell))
            {
                failReason = "未能为缪诺奖励成员找到合适的落点。";
                return false;
            }

            GenSpawn.Spawn(pawn, dropCell, map, WipeMode.Vanish);
            NormalizeJoinedPawnState(pawn);
            if (!IsJoinedPawnValid(pawn))
            {
                failReason = "新缪诺成员加入殖民地后未能恢复为玩家殖民者状态。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 生成一个可直接加入玩家阵营的缪诺殖民者。
        /// </summary>
        private static Pawn GenerateMunoColonist()
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                MunoDefDataRef.MunoRace_Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.PlayerStarter,
                -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 0f);

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            NormalizeJoinedPawnState(pawn);
            return pawn;
        }

        /// <summary>
        /// 将被上交的 Pawn 从远行队移除，并改归缪诺派系保留为世界 Pawn。
        /// </summary>
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

        /// <summary>
        /// 将新生成的缪诺成员安全加入当前远行队，并刷新远行队缓存。
        /// </summary>
        private static void AddGeneratedPawnToCaravan(Caravan caravan, Pawn pawn)
        {
            NormalizeJoinedPawnState(pawn);
            caravan.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
            caravan.Notify_PawnAdded(pawn);
            if (!pawn.IsWorldPawn())
            {
                // 先加入远行队再转为世界 Pawn，避免原版把自由状态的玩家人形 Pawn 改派到随机外部派系。
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }

            NormalizeJoinedPawnState(pawn);
        }

        /// <summary>
        /// 将交换得到的 Pawn 统一整理成玩家可控制殖民者状态，避免加入远行队时被自动当成囚犯。
        /// </summary>
        private static void NormalizeJoinedPawnState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            RecruitUtility.Recruit(pawn, Faction.OfPlayer);
            pawn.guest?.SetGuestStatus(null);
        }

        /// <summary>
        /// 校验新加入远行队的 Pawn 是否已经恢复为玩家殖民者，而不是囚犯或奴隶。
        /// </summary>
        private static bool IsJoinedPawnValid(Pawn pawn)
        {
            return pawn != null
                && pawn.Faction == Faction.OfPlayer
                && pawn.HostFaction == null
                && !pawn.IsPrisonerOfColony
                && !pawn.IsSlaveOfColony;
        }
    }
}
