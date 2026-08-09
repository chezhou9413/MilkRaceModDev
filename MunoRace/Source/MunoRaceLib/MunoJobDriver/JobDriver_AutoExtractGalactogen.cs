using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using MunoRaceLib.Tool;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobDriver
{
    //执行汲取内衬和挤奶器的自动产乳或自动浓浆流程，并把产物安全送入储存罐或背包。
    public class JobDriver_AutoExtractGalactogen : JobDriver
    {
        private const int MilkDurationTicks = 150;
        private const int SlurryDurationTicks = 200;

        //根据当前 JobDef 返回本次工作的自动汲取模式。
        private GalactogenAutoCollectMode Mode
        {
            get
            {
                return job.def == MunoDefDataRef.JobDef_AutoExtractConcentratedMulacte
                    ? GalactogenAutoCollectMode.ConcentratedSlurry
                    : GalactogenAutoCollectMode.Milk;
            }
        }

        //自动汲取只操作执行者自身，不需要占用地图目标。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        //生成等待、乳源质视觉效果和最终事务化产出的完整流程。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !CanContinueExtraction());

            Toil waitToil = Toils_General.Wait(Mode == GalactogenAutoCollectMode.Milk ? MilkDurationTicks : SlurryDurationTicks);
            waitToil.WithProgressBarToilDelay(TargetIndex.A);
            waitToil.handlingFacing = true;
            waitToil.tickAction = DrawExtractionEffects;
            yield return waitToil;

            yield return Toils_General.Do(CompleteExtraction);
        }

        //判断装备、模式和小人状态是否仍允许当前自动汲取工作继续。
        private bool CanContinueExtraction()
        {
            ThingComp_Galactogen galactogen = pawn.GetComp<ThingComp_Galactogen>();
            return galactogen != null
                && galactogen.autoCollectEnabled
                && galactogen.autoCollectMode == Mode
                && GalactogenExtractorUtility.HasActiveExtractor(pawn)
                && !pawn.Drafted
                && !pawn.Downed
                && !pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition);
        }

        //在等待期间生成与原有挤奶流程一致的飞溅和污物表现。
        private void DrawExtractionEffects()
        {
            if (pawn.Map == null)
            {
                return;
            }

            if (pawn.IsHashIntervalTick(5))
            {
                FilthGalactogenTool.SpawnMilkSplatter(pawn.Position.ToVector3Shifted(), pawn.Map, 4);
            }

            if (pawn.IsHashIntervalTick(30) && Rand.Chance(0.4f))
            {
                FilthGalactogenTool.SpawnFilthGalactogen(pawn);
            }
        }

        //在工作完成时按当前阈值重新校验可产数量，避免模式或资源变化造成透支。
        private void CompleteExtraction()
        {
            ThingComp_Galactogen galactogen = pawn.GetComp<ThingComp_Galactogen>();
            if (galactogen == null || !CanContinueExtraction())
            {
                return;
            }

            float targetAmount = galactogen.MaxGalactogen * galactogen.AutoGather;
            float surplus = Mathf.Max(0f, galactogen.CurrentGalactogen - targetAmount);
            if (Mode == GalactogenAutoCollectMode.Milk)
            {
                CompleteMilkExtraction(galactogen, Mathf.FloorToInt(surplus));
                return;
            }

            CompleteSlurryExtraction(galactogen, Mathf.FloorToInt(surplus / 100f));
        }

        //把自动产出的缪诺乳完整加入背包，成功后再扣除对应乳源质并刷新心情。
        private void CompleteMilkExtraction(ThingComp_Galactogen galactogen, int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (!TryAddStacksToInventory(MunoDefDataRef.MunoRace_MunoMilk, count))
            {
                Messages.Message(pawn.LabelShort + "的背包无法接收缪诺乳，自动汲取已取消。", pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            galactogen.ReMoveAutoGalactogen(count);
            MunoMilkingUtility.GiveMilkingMood(pawn);
            Messages.Message(pawn.LabelShort + "自动汲取了 " + count + " 份缪诺乳。", pawn, MessageTypeDefOf.PositiveEvent);
        }

        //把自动产出的浓浆先装入共享储存罐，溢出部分完整加入背包，全部成功后再扣除乳源质。
        private void CompleteSlurryExtraction(ThingComp_Galactogen galactogen, int count)
        {
            if (count <= 0)
            {
                return;
            }

            int stored = GalactogenStorageUtility.AddToSharedReserve(pawn, count);
            int overflow = count - stored;
            if (overflow > 0 && !TryAddStacksToInventory(MunoDefDataRef.MunoRace_ConcentratedMulacte, overflow))
            {
                GalactogenStorageUtility.RemoveFromSharedReserve(pawn, stored);
                Messages.Message(pawn.LabelShort + "的背包无法接收乳源质浓浆，自动汲取已取消。", pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            galactogen.ReMoveAutoGalactogen(count * 100f);
            Messages.Message(pawn.LabelShort + "自动汲取了 " + count + " 份乳源质浓浆。", pawn, MessageTypeDefOf.PositiveEvent);
        }

        //按物品堆叠上限创建一个或多个堆并事务化加入背包，任意一堆失败时回滚全部本次产物。
        private bool TryAddStacksToInventory(ThingDef thingDef, int totalCount)
        {
            if (thingDef == null || pawn.inventory?.innerContainer == null)
            {
                return false;
            }

            List<Thing> addedThings = new List<Thing>();
            int remaining = totalCount;
            while (remaining > 0)
            {
                Thing thing = ThingMaker.MakeThing(thingDef);
                thing.stackCount = Mathf.Min(remaining, thingDef.stackLimit);
                if (!pawn.inventory.innerContainer.TryAdd(thing, canMergeWithExistingStacks: false))
                {
                    thing.Destroy();
                    RollbackInventoryThings(addedThings);
                    return false;
                }

                addedThings.Add(thing);
                remaining -= thing.stackCount;
            }

            return true;
        }

        //从背包移除并销毁本次已经加入的临时产物，保持乳源质与物品状态一致。
        private static void RollbackInventoryThings(List<Thing> things)
        {
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                thing.holdingOwner?.Remove(thing);
                thing.Destroy();
            }
        }
    }
}
