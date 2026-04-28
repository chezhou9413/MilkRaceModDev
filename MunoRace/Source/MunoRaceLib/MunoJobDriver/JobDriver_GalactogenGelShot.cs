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
    /// <summary>
    /// 负责在原版能力瞄准与暖机后，以固定间隔连续发射三枚化合粘胶弹。
    /// </summary>
    public class JobDriver_GalactogenGelShot : JobDriver_CastVerbOnce
    {
        private int shotsFired;
        private int ticksUntilNextShot;
        private CompAbilityEffect_GalactogenGelShot gelEffect;

        /// <summary>
        /// 构建化合粘胶弹能力的瞄准、暖机和三发连续射击流程。
        /// </summary>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !job.ability.CanCast && !job.ability.Casting);
            Toil stopMoving = ToilMaker.MakeToil("StopMovingForGalactogenGelShot");
            stopMoving.initAction = delegate
            {
                pawn.pather.StopDead();
                gelEffect = job.ability?.CompOfType<CompAbilityEffect_GalactogenGelShot>();
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

            Toil burstFire = ToilMaker.MakeToil("FireGalactogenGelBurst");
            burstFire.initAction = delegate
            {
                shotsFired = 0;
                ticksUntilNextShot = 0;
                if (job.count != 1)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
            };
            burstFire.tickAction = delegate
            {
                if (gelEffect == null || pawn.Map == null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                MaintainAimingStance();
                if (ticksUntilNextShot > 0)
                {
                    ticksUntilNextShot--;
                    return;
                }

                FireOneGelShot(shotsFired);
                shotsFired++;
                if (shotsFired >= gelEffect.BurstCount)
                {
                    ReadyForNextToil();
                    return;
                }

                ticksUntilNextShot = gelEffect.TicksBetweenShots - 1;
            };
            burstFire.defaultCompleteMode = ToilCompleteMode.Never;
            burstFire.activeSkill = () => SkillDefOf.Shooting;
            yield return burstFire;
        }

        /// <summary>
        /// 通知能力已进入施放流程，保持原版能力暖机与效果器状态一致。
        /// </summary>
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.ability?.Notify_StartedCasting();
        }

        /// <summary>
        /// 发射一枚带随机散布和高抛贝塞尔曲线的化合粘胶弹。
        /// </summary>
        private void FireOneGelShot(int shotIndex)
        {
            LocalTargetInfo target = job.GetTarget(TargetIndex.A);
            IntVec3 destination = RandomBurstDestination(target.Cell);
            Projectile_GalactogenGel projectile = (Projectile_GalactogenGel)GenSpawn.Spawn(MunoDefDataRef.Bullet_MunoAC_Gel, pawn.Position, pawn.Map);
            projectile.ConfigureCurve(shotIndex - 1, Rand.Range(1.35f, 1.9f));
            projectile.Launch(pawn, pawn.DrawPos, destination, target, ProjectileHitFlags.IntendedTarget | ProjectileHitFlags.NonTargetWorld, false, pawn.equipment?.Primary);
            if (job.ability.def.verbProperties.soundCast != null)
            {
                SoundInfo soundInfo = SoundInfo.InMap(new TargetInfo(pawn.Position, pawn.Map));
                soundInfo.volumeFactor = 2f;
                job.ability.def.verbProperties.soundCast.PlayOneShot(soundInfo);
            }
        }

        /// <summary>
        /// 在三连发尚未结束时持续面向目标并维持举枪瞄准姿态。
        /// </summary>
        private void MaintainAimingStance()
        {
            LocalTargetInfo target = job.GetTarget(TargetIndex.A);
            if (target.IsValid)
            {
                pawn.rotationTracker.FaceTarget(target);
            }

            if (!(pawn.stances.curStance is Stance_Cooldown cooldown) || cooldown.verb != job.verbToUse || cooldown.focusTarg != target || cooldown.ticksLeft <= 2)
            {
                pawn.stances.SetStance(new Stance_Cooldown(4, target, job.verbToUse));
            }
        }

        /// <summary>
        /// 为每一枚粘胶弹生成不同落点，避免三发命中同一格。
        /// </summary>
        private IntVec3 RandomBurstDestination(IntVec3 targetCell)
        {
            float radius = gelEffect?.ForcedMissRadius ?? 0f;
            int maxExclusive = GenRadial.NumCellsInRadius(radius);
            if (maxExclusive <= 1)
            {
                return targetCell;
            }

            for (int i = 0; i < 8; i++)
            {
                IntVec3 cell = targetCell + GenRadial.RadialPattern[Rand.Range(1, maxExclusive)];
                if (cell.IsValid && cell != targetCell)
                {
                    return cell;
                }
            }

            return targetCell;
        }
    }
}
