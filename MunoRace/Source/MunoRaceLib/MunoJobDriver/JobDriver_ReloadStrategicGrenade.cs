using MunoRaceLib.MunoComp;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobDriver
{
    //执行为缪诺战略步枪榴弹仓装填钢铁的搬运与填充流程。
    public class JobDriver_ReloadStrategicGrenade : JobDriver
    {
        private const TargetIndex SelfInd = TargetIndex.A;
        private const TargetIndex SteelInd = TargetIndex.B;
        private const int FillTicks = 120;

        private Thing Steel
        {
            get { return job.GetTarget(SteelInd).Thing; }
        }

        private Comp_StrategicGrenadeStorageWeapon WeaponComp
        {
            get
            {
                ThingWithComps primary = pawn?.equipment?.Primary;
                return primary?.GetComp<Comp_StrategicGrenadeStorageWeapon>();
            }
        }

        //预留将要搬运的钢铁，避免多个小人抢同一组材料。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Steel, job, 1, -1, null, errorOnFailed);
        }

        //生成前往钢铁、搬运、等待装填并写入榴弹仓的 Toil 序列。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(SteelInd);

            AddEndCondition(delegate
            {
                Comp_StrategicGrenadeStorageWeapon comp = WeaponComp;
                return (comp == null || comp.AmmoFull) ? JobCondition.Succeeded : JobCondition.Ongoing;
            });

            yield return Toils_Goto.GotoThing(SteelInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(SteelInd)
                .FailOnSomeonePhysicallyInteracting(SteelInd);

            yield return Toils_Haul.StartCarryThing(SteelInd, false, true);
            yield return Toils_General.Wait(FillTicks).WithProgressBarToilDelay(SelfInd);

            yield return new Toil
            {
                initAction = delegate
                {
                    Comp_StrategicGrenadeStorageWeapon comp = WeaponComp;
                    if (comp == null || comp.AmmoFull)
                    {
                        return;
                    }

                    Thing carried = pawn.carryTracker.CarriedThing;
                    if (carried == null || carried.def != ThingDefOf.Steel)
                    {
                        return;
                    }

                    int missingAmmo = comp.AmmoCapacity - comp.AmmoCount;
                    int ammoFromSteel = carried.stackCount / comp.SteelPerGrenade;
                    int ammoToLoad = Mathf.Min(missingAmmo, ammoFromSteel);
                    if (ammoToLoad <= 0)
                    {
                        return;
                    }

                    int loaded = comp.AddAmmo(ammoToLoad);
                    carried.stackCount -= loaded * comp.SteelPerGrenade;
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
