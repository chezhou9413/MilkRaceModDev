using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobDriver
{
    //把搬运到手的乳源质浓浆装入当前下一件未装满的穿戴装备。
    public class JobDriver_RefuelGalactogenArmor : JobDriver
    {
        private const TargetIndex SelfInd = TargetIndex.A;
        private const TargetIndex FuelInd = TargetIndex.B;
        private const int FillTicks = 120;

        private Thing Fuel
        {
            get { return job.GetTarget(FuelInd).Thing; }
        }

        private Comp_GalactogenStorageArmor ArmorComp
        {
            get
            {
                return GalactogenStorageUtility.FindNextRefuelableApparelStorage(pawn);
            }
        }

        //预留需要搬运的浓浆物品。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Fuel, job, 1, -1, null, errorOnFailed);
        }

        //依次执行靠近、搬运、等待和装填，并在目标装备装满后安全结束。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(FuelInd);

            AddEndCondition(delegate
            {
                Comp_GalactogenStorageArmor comp = ArmorComp;
                return (comp == null || comp.SlotFull) ? JobCondition.Succeeded : JobCondition.Ongoing;
            });

            yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(FuelInd)
                .FailOnSomeonePhysicallyInteracting(FuelInd);

            yield return Toils_Haul.StartCarryThing(FuelInd, false, true);

            yield return Toils_General.Wait(FillTicks).WithProgressBarToilDelay(SelfInd);

            yield return new Toil
            {
                initAction = delegate
                {
                    Comp_GalactogenStorageArmor comp = ArmorComp;
                    if (comp == null || comp.SlotFull)
                    {
                        return;
                    }

                    Thing carried = pawn.carryTracker.CarriedThing;
                    if (carried == null || carried.def != MunoDefDataRef.MunoRace_ConcentratedMulacte)
                    {
                        return;
                    }

                    int toLoad = Mathf.Min(carried.stackCount, comp.SlotCapacity - comp.SlotCount);
                    if (toLoad <= 0)
                    {
                        return;
                    }

                    int loaded = comp.AddSlot(toLoad);
                    carried.stackCount -= loaded;

                    if (carried.stackCount <= 0)
                    {
                        pawn.carryTracker.DestroyCarriedThing();
                    }
                    else
                    {
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
