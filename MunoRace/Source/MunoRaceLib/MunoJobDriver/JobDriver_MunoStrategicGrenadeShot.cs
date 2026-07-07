using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MunoRaceLib.MunoJobDriver
{
    //负责在原版能力瞄准与暖机后发射一枚战略榴弹。
    public class JobDriver_MunoStrategicGrenadeShot : JobDriver_CastVerbOnce
    {
        private CompAbilityEffect_MunoStrategicGrenadeShot grenadeEffect;

        //构建战略榴弹能力的瞄准、暖机和弹丸发射流程。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !job.ability.CanCast && !job.ability.Casting);

            Toil stopMoving = ToilMaker.MakeToil("StopMovingForMunoStrategicGrenadeShot");
            stopMoving.initAction = delegate
            {
                pawn.pather.StopDead();
                grenadeEffect = job.ability?.CompOfType<CompAbilityEffect_MunoStrategicGrenadeShot>();
                job.count = 0;
            };
            stopMoving.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return stopMoving;

            Toil warmupAndAuthorize = Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, canHitNonTargetPawns: false);
            if (job.ability != null && job.ability.def.showCastingProgressBar && job.verbToUse != null)
            {
                warmupAndAuthorize.WithProgressBar(TargetIndex.A, () => job.verbToUse.WarmupProgress);
            }
            yield return warmupAndAuthorize;

            yield return new Toil
            {
                initAction = delegate
                {
                    if (job.count != 1 || grenadeEffect == null || pawn.Map == null)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }

                    FireGrenade();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        //通知能力已进入施放流程，保持原版能力暖机与效果器状态一致。
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.ability?.Notify_StartedCasting();
        }

        //发射一枚带随机散布的战略榴弹。
        private void FireGrenade()
        {
            LocalTargetInfo target = job.GetTarget(TargetIndex.A);
            IntVec3 destination = RandomDestination(target.Cell);
            Projectile projectile = (Projectile)GenSpawn.Spawn(MunoDefDataRef.Bullet_MunoSR_Grenade, pawn.Position, pawn.Map);
            projectile.Launch(pawn, pawn.DrawPos, destination, target, ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetWorld, false, pawn.equipment?.Primary);

            SoundDef soundCast = job.ability?.def?.verbProperties?.soundCast;
            if (soundCast != null)
            {
                SoundInfo soundInfo = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map));
                soundInfo.volumeFactor = 1.4f;
                soundCast.PlayOneShot(soundInfo);
            }
        }

        //为战略榴弹生成带散布的落点。
        private IntVec3 RandomDestination(IntVec3 targetCell)
        {
            float radius = grenadeEffect?.ForcedMissRadius ?? 0f;
            int maxExclusive = GenRadial.NumCellsInRadius(radius);
            if (maxExclusive <= 1)
            {
                return targetCell;
            }

            for (int i = 0; i < 8; i++)
            {
                IntVec3 cell = targetCell + GenRadial.RadialPattern[Rand.Range(1, maxExclusive)];
                if (cell.IsValid)
                {
                    return cell;
                }
            }

            return targetCell;
        }
    }
}
