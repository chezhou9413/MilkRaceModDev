using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    /// <summary>
    /// 缪诺治疗枪的目标效果发射器，负责目标合法性判断、瞄准反馈与治疗弹发射。
    /// </summary>
    public class Verb_CastTargetEffect_MunoHealProjectile : Verb_CastTargetEffect
    {
        /// <summary>
        /// 在瞄准界面绘制鼠标反馈，并提示当前目标是否可被治疗枪作用。
        /// </summary>
        public override void OnGUI(LocalTargetInfo target)
        {
            if (!CanAffectTargetNow(target))
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
                return;
            }

            base.OnGUI(target);
        }

        /// <summary>
        /// 绘制治疗枪的瞄准高亮，避免沿用原版远程武器逐格射线判定的高开销射程圈。
        /// </summary>
        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (target.IsValid && CanAffectTargetNow(target))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }

            GenDraw.DrawRadiusRing(caster.Position, EffectiveRange);
        }

        /// <summary>
        /// 在真正执行指令前校验目标，防止不可治疗或无法命中的目标进入施法流程。
        /// </summary>
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!CanAffectTargetNow(target))
            {
                return false;
            }

            return base.ValidateTarget(target, showMessages);
        }

        /// <summary>
        /// 发射治疗弹射体，并在成功发射后消耗一次装填资源。
        /// </summary>
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

        /// <summary>
        /// 判断当前目标此刻是否满足射线、目标类型与治疗效果三类条件。
        /// </summary>
        private bool CanAffectTargetNow(LocalTargetInfo target)
        {
            if (!target.IsValid || caster?.Map == null)
            {
                return false;
            }

            if (!CanHitTarget(target) || !verbProps.targetParams.CanTarget(target.ToTargetInfo(caster.Map)))
            {
                return false;
            }

            if (target.Thing == null)
            {
                return false;
            }

            foreach (CompTargetEffect comp in base.EquipmentSource.GetComps<CompTargetEffect>())
            {
                if (!comp.CanApplyOn(target.Thing))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 缪诺治疗弹射体，负责飞行拖尾与命中后的治疗效果触发。
    /// </summary>
    public class Projectile_MunoHealing : Projectile
    {
        private static readonly Color TrailColor = new Color(0.2f, 1f, 0.35f);

        /// <summary>
        /// 推进治疗弹飞行、生成拖尾并在到达目标时触发命中逻辑。
        /// </summary>
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

        /// <summary>
        /// 在治疗弹飞行过程中生成绿色拖尾粒子，增强治疗反馈。
        /// </summary>
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

        /// <summary>
        /// 在命中时对目标执行治疗枪附带的所有目标效果组件。
        /// </summary>
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
