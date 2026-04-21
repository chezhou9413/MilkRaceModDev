using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    public class Verb_CastTargetEffect_MunoHealProjectile : Verb_CastTargetEffect
    {
        public override void OnGUI(LocalTargetInfo target)
        {
            if (!CanHitTarget(target) || !verbProps.targetParams.CanTarget(target.ToTargetInfo(caster.Map)))
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
                return;
            }

            if (target.Thing != null)
            {
                foreach (CompTargetEffect comp in base.EquipmentSource.GetComps<CompTargetEffect>())
                {
                    if (!comp.CanApplyOn(target.Thing))
                    {
                        GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
                        return;
                    }
                }
            }
            else
            {
                base.OnGUI(target);
            }
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Thing != null)
            {
                foreach (CompTargetEffect comp in base.EquipmentSource.GetComps<CompTargetEffect>())
                {
                    if (!comp.CanApplyOn(target.Thing))
                    {
                        return false;
                    }
                }
            }

            return base.ValidateTarget(target, showMessages);
        }

        protected override bool TryCastShot()
        {
            Pawn casterPawn = CasterPawn;
            if (casterPawn == null || currentTarget.Thing == null || verbProps.defaultProjectile == null)
            {
                return false;
            }

            Projectile projectile = (Projectile)GenSpawn.Spawn(verbProps.defaultProjectile, casterPawn.Position, casterPawn.Map);
            projectile.Launch(casterPawn, casterPawn.DrawPos, currentTarget, currentTarget, ProjectileHitFlags.IntendedTarget, preventFriendlyFire, base.EquipmentSource);
            base.ReloadableCompSource?.UsedOnce();
            return true;
        }
    }

    public class Projectile_MunoHealing : Projectile
    {
        private static readonly Color TrailColor = new Color(0.2f, 1f, 0.35f);

        protected override void TickInterval(int delta)
        {
            for (int i = 0; i < AllComps.Count; i++)
            {
                AllComps[i].CompTickInterval(delta);
            }

            lifetime -= delta;
            if (landed)
            {
                return;
            }

            ticksToImpact -= delta;
            if (!ExactPosition.InBounds(base.Map))
            {
                ticksToImpact += delta;
                base.Position = ExactPosition.ToIntVec3();
                Destroy();
                return;
            }

            base.Position = ExactPosition.ToIntVec3();
            ThrowHealingTrail();

            if (ticksToImpact <= 0)
            {
                if (DestinationCell.InBounds(base.Map))
                {
                    base.Position = DestinationCell;
                }

                Impact(intendedTarget.Thing ?? usedTarget.Thing);
            }
        }

        private void ThrowHealingTrail()
        {
            if (base.Map == null || !ExactPosition.ShouldSpawnMotesAt(base.Map))
            {
                return;
            }

            FleckCreationData data = FleckMaker.GetDataStatic(ExactPosition, base.Map, FleckDefOf.MicroSparksFast, 0.22f);
            data.instanceColor = TrailColor;
            data.rotationRate = Rand.Range(-8f, 8f);
            data.velocityAngle = ExactRotation.eulerAngles.y + 180f + Rand.Range(-12f, 12f);
            data.velocitySpeed = 0.02f;
            base.Map.flecks.CreateFleck(data);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn casterPawn = launcher as Pawn;
            Thing actualTarget = hitThing ?? intendedTarget.Thing ?? usedTarget.Thing;

            if (casterPawn != null && actualTarget != null && equipment is ThingWithComps thingWithComps)
            {
                foreach (CompTargetEffect comp in thingWithComps.GetComps<CompTargetEffect>())
                {
                    if (comp.CanApplyOn(actualTarget))
                    {
                        comp.DoEffectOn(casterPawn, actualTarget);
                    }
                }
            }

            base.Impact(actualTarget, blockedByShield);
        }
    }
}
