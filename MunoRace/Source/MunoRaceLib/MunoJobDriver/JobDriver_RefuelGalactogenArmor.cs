using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobDriver
{
    /// <summary>
    /// 穿戴者走去拾取乳原质浓浆并装入盔甲槽。
    /// TargetA = pawn 自身（进度条锚点）
    /// TargetB = 要拾取的浓浆物品
    /// </summary>
    public class JobDriver_RefuelGalactogenArmor : JobDriver
    {
        private const TargetIndex SelfInd = TargetIndex.A;
        private const TargetIndex FuelInd = TargetIndex.B;
        private const int FillTicks = 120;

        private Thing Fuel => job.GetTarget(FuelInd).Thing;

        private ThingComp_GalactogenArmor ArmorComp
        {
            get
            {
                if (pawn?.apparel == null) return null;
                foreach (Apparel ap in pawn.apparel.WornApparel)
                {
                    ThingComp_GalactogenArmor comp = ap.GetComp<ThingComp_GalactogenArmor>();
                    if (comp != null) return comp;
                }
                return null;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Fuel, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(FuelInd);

            // 槽满或盔甲丢失时提前结束
            AddEndCondition(() =>
            {
                ThingComp_GalactogenArmor comp = ArmorComp;
                return (comp == null || comp.SlotFull)
                    ? JobCondition.Succeeded
                    : JobCondition.Ongoing;
            });

            // 1. 走到浓浆处
            yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(FuelInd)
                .FailOnSomeonePhysicallyInteracting(FuelInd);

            // 2. 拿起浓浆
            yield return Toils_Haul.StartCarryThing(FuelInd,
                putRemainderInQueue: false,
                subtractNumTakenFromJobCount: true);

            // 3. 装填等待（进度条显示在 pawn 头上）
            yield return Toils_General.Wait(FillTicks)
                .WithProgressBarToilDelay(SelfInd);

            // 4. 写入槽位
            yield return MakeLoadToil();
        }

        private Toil MakeLoadToil()
        {
            return new Toil
            {
                initAction = () =>
                {
                    ThingComp_GalactogenArmor comp = ArmorComp;
                    if (comp == null || comp.SlotFull) return;

                    Thing carried = pawn.carryTracker.CarriedThing;
                    if (carried == null || carried.def != MunoDefDataRef.MunoRace_ConcentratedMulacte)
                        return;

                    int toLoad = Mathf.Min(carried.stackCount, comp.SlotCapacity - comp.SlotCount);
                    if (toLoad <= 0) return;

                    int loaded = comp.AddSlot(toLoad);
                    carried.stackCount -= loaded;

                    if (carried.stackCount <= 0)
                        pawn.carryTracker.DestroyCarriedThing();
                    else
                        // 剩余的放回地上
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}