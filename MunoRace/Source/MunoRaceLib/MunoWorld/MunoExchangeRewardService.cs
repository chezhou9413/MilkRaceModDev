using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责生成缪诺交换奖励，并将批量奖励投放到地图或远行队。
    public static class MunoExchangeRewardService
    {
        //按目标数量生成可直接加入玩家阵营的缪诺殖民者。
        public static bool TryGenerateMunoPawns(int count, out List<Pawn> pawns, out string failReason)
        {
            pawns = new List<Pawn>();
            failReason = null;
            if (count < 0)
            {
                failReason = "缪诺奖励数量无效。";
                return false;
            }

            if (count == 0)
            {
                return true;
            }

            if (MunoDefDataRef.MunoRace_Colonist == null)
            {
                failReason = "缪诺殖民者模板缺失，无法生成奖励成员。";
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                Pawn pawn = GenerateMunoColonist();
                if (pawn == null)
                {
                    DestroyPawns(pawns);
                    pawns.Clear();
                    failReason = "未能生成新的缪诺成员。";
                    return false;
                }

                pawns.Add(pawn);
            }

            return true;
        }

        //按选择物资奖励的目标市场价值总和生成一份原版任务式随机物资包。
        public static bool TryGenerateRandomItemReward(List<MunoExchangeTargetRecord> targets, out List<Thing> items, out float totalPawnValue, out string failReason)
        {
            items = new List<Thing>();
            totalPawnValue = 0f;
            failReason = null;
            if (targets == null)
            {
                failReason = "交换目标列表无效，无法生成随机物资奖励。";
                return false;
            }

            int randomItemTargetCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                MunoExchangeTargetRecord target = targets[i];
                if (target == null)
                {
                    failReason = "交换目标列表包含无效记录。";
                    return false;
                }

                if (target.rewardType != MunoExchangeRewardType.RandomItems)
                {
                    continue;
                }

                randomItemTargetCount++;

                if (target.pawn == null || target.pawn.Destroyed)
                {
                    failReason = "随机物资奖励对应的交换目标已失效。";
                    return false;
                }

                totalPawnValue += target.pawn.MarketValue;
            }

            if (randomItemTargetCount == 0)
            {
                return true;
            }

            if (totalPawnValue <= 0f)
            {
                failReason = "随机物资奖励价值无效。";
                return false;
            }

            Faction giverFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            if (giverFaction == null)
            {
                failReason = "缪诺派系数据无效，无法生成随机物资奖励。";
                return false;
            }

            RewardsGeneratorParams parms = new RewardsGeneratorParams
            {
                rewardValue = totalPawnValue,
                giverFaction = giverFaction,
                minGeneratedRewardValue = 250f,
                thingRewardRequired = true,
                thingRewardItemsOnly = true,
                allowGoodwill = false,
                allowRoyalFavor = false,
                allowDevelopmentPoints = false,
                allowXenogermReimplantation = false
            };

            List<Reward> rewards = RewardsGenerator.Generate(parms, out float generatedRewardValue);
            for (int i = 0; i < rewards.Count; i++)
            {
                Reward_Items itemReward = rewards[i] as Reward_Items;
                if (itemReward == null)
                {
                    continue;
                }

                items.AddRange(itemReward.ItemsListForReading);
            }

            if (items.Count == 0)
            {
                failReason = "原版奖励生成器未能生成有效物资。";
                return false;
            }

            if (generatedRewardValue <= 0f)
            {
                DestroyItems(items);
                items.Clear();
                failReason = "随机物资奖励价值无效。";
                return false;
            }

            return true;
        }

        //将批量缪诺成员与物资奖励一并投放到玩家地图。
        public static bool TryDeliverRewardsToMap(Map map, List<Pawn> pawns, List<Thing> items, out string failReason)
        {
            failReason = null;
            if (map == null)
            {
                failReason = "地图无效，无法投放缪诺交换奖励。";
                return false;
            }

            List<Thing> things = new List<Thing>();
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null || pawn.Destroyed)
                    {
                        DestroyThings(things);
                        failReason = "缪诺奖励成员已失效，无法完成投放。";
                        return false;
                    }

                    NormalizeJoinedPawnState(pawn);
                    things.Add(pawn);
                }
            }

            if (!AreItemsReadyForDelivery(items))
            {
                DestroyThings(things);
                failReason = "随机物资奖励已失效，无法完成地图投放。";
                return false;
            }

            AddValidItems(items, things);
            if (things.Count == 0)
            {
                failReason = "本次交换没有可投放的奖励。";
                return false;
            }

            if (!DropCellFinder.TryFindDropSpotNear(map.Center, map, out IntVec3 dropCell, allowFogged: false, canRoofPunch: false))
            {
                DestroyThings(things);
                failReason = "未能为缪诺交换奖励找到合适的落点。";
                return false;
            }

            DropPodUtility.DropThingsNear(dropCell, map, things, 110, canInstaDropDuringInit: false, leaveSlag: false, canRoofPunch: false, forbid: false, allowFogged: false, Faction.OfPlayer);
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    NormalizeJoinedPawnState(pawns[i]);
                    if (!IsJoinedPawnValid(pawns[i]))
                    {
                        failReason = "缪诺奖励成员未能正确加入玩家殖民地。";
                        return false;
                    }
                }
            }

            return true;
        }

        //将批量缪诺成员加入远行队，并将物资奖励分配到远行队库存。
        public static bool TryDeliverRewardsToCaravan(Caravan caravan, List<Pawn> pawns, List<Thing> items, out string failReason, List<Pawn> excludedItemCarriers = null)
        {
            failReason = null;
            if (caravan == null)
            {
                failReason = "远行队无效，无法加入缪诺交换奖励。";
                return false;
            }

            if (!AreItemsReadyForDelivery(items))
            {
                failReason = "随机物资奖励已失效，无法完成加入。";
                return false;
            }

            List<Pawn> addedPawns = new List<Pawn>();
            List<Thing> addedItems = new List<Thing>();
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null || pawn.Destroyed)
                    {
                        RemoveRewardPawnsFromCaravan(caravan, addedPawns);
                        failReason = "缪诺奖励成员已失效，无法完成加入。";
                        return false;
                    }

                    AddGeneratedPawnToCaravan(caravan, pawn);
                    addedPawns.Add(pawn);
                    if (!IsJoinedPawnValid(pawn) || !caravan.ContainsPawn(pawn))
                    {
                        RemoveRewardPawnsFromCaravan(caravan, addedPawns);
                        failReason = "缪诺奖励成员未能正确加入远行队。";
                        return false;
                    }
                }
            }

            List<Pawn> itemCarriers = new List<Pawn>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    Thing item = items[i];
                    Pawn carrier = CaravanInventoryUtility.FindPawnToMoveInventoryTo(item, caravan.PawnsListForReading, excludedItemCarriers);
                    if (carrier == null)
                    {
                        RollbackCaravanRewards(caravan, addedPawns, addedItems);
                        failReason = "远行队没有可保留随机物资奖励的成员。";
                        return false;
                    }

                    itemCarriers.Add(carrier);
                }

                for (int i = 0; i < items.Count; i++)
                {
                    Thing item = items[i];
                    bool added = excludedItemCarriers.NullOrEmpty()
                        ? GiveItemThroughCaravanUtility(caravan, item)
                        : itemCarriers[i].inventory.innerContainer.TryAdd(item, canMergeWithExistingStacks: false);
                    if (!added)
                    {
                        RollbackCaravanRewards(caravan, addedPawns, addedItems);
                        failReason = "随机物资奖励未能加入远行队库存。";
                        return false;
                    }

                    if (!item.Destroyed)
                    {
                        addedItems.Add(item);
                    }
                }
            }

            return true;
        }

        //在没有交换目标排除条件时沿用原版远行队物资分配流程。
        private static bool GiveItemThroughCaravanUtility(Caravan caravan, Thing item)
        {
            CaravanInventoryUtility.GiveThing(caravan, item);
            return item.Destroyed || item.ParentHolder != null;
        }

        //销毁尚未放入地图或远行队的奖励 Pawn。
        public static void DestroyPawns(List<Pawn> pawns)
        {
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed && pawn.ParentHolder == null)
                {
                    pawn.Destroy();
                }
            }
        }

        //销毁尚未交付的随机物资。
        public static void DestroyItems(List<Thing> items)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                Thing item = items[i];
                if (item != null && !item.Destroyed && item.ParentHolder == null)
                {
                    item.Destroy();
                }
            }
        }

        //销毁尚未交付的奖励对象。
        private static void DestroyThings(List<Thing> things)
        {
            if (things == null)
            {
                return;
            }

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && !thing.Destroyed && thing.ParentHolder == null)
                {
                    thing.Destroy();
                }
            }
        }

        //将有效的物资加入统一投放列表。
        private static void AddValidItems(List<Thing> items, List<Thing> things)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                Thing item = items[i];
                if (item != null && !item.Destroyed && item.ParentHolder == null)
                {
                    things.Add(item);
                }
            }
        }

        //校验待发放物资仍未被销毁、生成或放入其他容器。
        private static bool AreItemsReadyForDelivery(List<Thing> items)
        {
            if (items == null)
            {
                return true;
            }

            for (int i = 0; i < items.Count; i++)
            {
                Thing item = items[i];
                if (item == null || item.Destroyed || item.ParentHolder != null)
                {
                    return false;
                }
            }

            return true;
        }

        //生成一名缪诺奖励殖民者并恢复玩家控制状态。
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

        //将生成的缪诺成员加入远行队并刷新远行队通知。
        private static void AddGeneratedPawnToCaravan(Caravan caravan, Pawn pawn)
        {
            NormalizeJoinedPawnState(pawn);
            caravan.AddPawn(pawn, addCarriedPawnToWorldPawnsIfAny: true);
            caravan.Notify_PawnAdded(pawn);
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }

            NormalizeJoinedPawnState(pawn);
        }

        //将生成的 Pawn 统一整理成玩家可控制殖民者。
        private static void NormalizeJoinedPawnState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            RecruitUtility.Recruit(pawn, Faction.OfPlayer);
            pawn.guest?.SetGuestStatus(null);
        }

        //校验生成的 Pawn 是否已经成为玩家殖民者。
        private static bool IsJoinedPawnValid(Pawn pawn)
        {
            return pawn != null
                && pawn.Faction == Faction.OfPlayer
                && pawn.HostFaction == null
                && !pawn.IsPrisonerOfColony
                && !pawn.IsSlaveOfColony;
        }

        //从远行队移除本次已经加入但未能完成事务的奖励成员。
        private static void RemoveRewardPawnsFromCaravan(Caravan caravan, List<Pawn> pawns)
        {
            if (caravan == null || pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && caravan.ContainsPawn(pawn))
                {
                    caravan.RemovePawn(pawn);
                    caravan.Notify_PawnRemoved(pawn);
                    if (!pawn.Destroyed)
                    {
                        pawn.Destroy();
                    }
                }
            }
        }

        //回滚本次已经加入远行队的奖励 Pawn 和物资，避免失败时留下部分奖励。
        private static void RollbackCaravanRewards(Caravan caravan, List<Pawn> pawns, List<Thing> items)
        {
            if (caravan == null)
            {
                return;
            }

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    Thing item = items[i];
                    if (item == null || item.Destroyed)
                    {
                        continue;
                    }

                    item.holdingOwner?.Remove(item);
                    item.Destroy();
                }
            }

            RemoveRewardPawnsFromCaravan(caravan, pawns);
        }
    }
}
