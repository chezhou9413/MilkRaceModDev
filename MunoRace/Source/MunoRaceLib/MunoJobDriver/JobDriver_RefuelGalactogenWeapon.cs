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
    /// 执行为当前主武器装填乳源质浓浆的搬运与填充流程。
    /// </summary>
    public class JobDriver_RefuelGalactogenWeapon : JobDriver
    {
        private const TargetIndex SelfInd = TargetIndex.A;
        private const TargetIndex FuelInd = TargetIndex.B;
        private const int FillTicks = 120;

        private Thing Fuel
        {
            get { return job.GetTarget(FuelInd).Thing; }
        }

        private Comp_GalactogenStorageWeapon WeaponComp
        {
            get
            {
                ThingWithComps primary = pawn?.equipment?.Primary;
                return primary?.GetComp<Comp_GalactogenStorageWeapon>();
            }
        }

        /// <summary>
        /// 预留将要搬运的浓浆物品，避免多个小人抢同一份燃料。
        /// </summary>
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Fuel, job, 1, -1, null, errorOnFailed);
        }

        /// <summary>
        /// 生成前往浓浆、搬运、等待装填并写入武器槽位的 Toil 序列。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(FuelInd);

            AddEndCondition(delegate
            {
                Comp_GalactogenStorageWeapon comp = WeaponComp;
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
                    Comp_GalactogenStorageWeapon comp = WeaponComp;
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
