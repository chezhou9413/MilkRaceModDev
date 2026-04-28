using MunoRaceLib.MunoDefRef;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    /// <summary>
    /// 使用原版能力系统承载缪诺机炮的化合粘胶弹 Gizmo 与目标选择。
    /// </summary>
    public class Ability_GalactogenGelShot : Ability
    {
        /// <summary>
        /// 供存档反序列化使用的默认构造函数。
        /// </summary>
        public Ability_GalactogenGelShot()
        {
        }

        /// <summary>
        /// 为指定小人创建绑定武器浓浆槽的化合粘胶弹能力。
        /// </summary>
        public Ability_GalactogenGelShot(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        /// <summary>
        /// 检查机炮浓浆槽是否允许当前能力按钮使用。
        /// </summary>
        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted)
                {
                    return baseReport;
                }

                Comp_GalactogenStorageWeapon comp = GelStorageComp();
                if (comp == null)
                {
                    return "未装备可发射化合粘胶弹的机炮。";
                }

                if (!comp.HasEnough(1))
                {
                    return "机炮浓浆槽不足。";
                }

                return true;
            }
        }

        /// <summary>
        /// 获取当前主武器上的机炮浓浆储存组件。
        /// </summary>
        private Comp_GalactogenStorageWeapon GelStorageComp()
        {
            return pawn?.equipment?.Primary?.GetComp<Comp_GalactogenStorageWeapon>();
        }

        /// <summary>
        /// 创建自定义发射 Job，让原版能力瞄准后进入三发间隔射击流程。
        /// </summary>
        public override Job GetJob(LocalTargetInfo target, LocalTargetInfo destination)
        {
            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_GalactogenGelShot, target, destination);
            job.ability = this;
            job.verbToUse = verb;
            return job;
        }
    }

    /// <summary>
    /// 显示化合粘胶弹能力按钮，并在右上角标出武器浓浆槽余量。
    /// </summary>
    public class Command_AbilityGalactogenGelShot : Command_Ability
    {
        /// <summary>
        /// 绑定原版能力命令所需的能力对象与小人。
        /// </summary>
        public Command_AbilityGalactogenGelShot(Ability ability, Pawn pawn) : base(ability, pawn)
        {
        }

        /// <summary>
        /// 返回当前机炮浓浆槽数量，显示在能力按钮右上角。
        /// </summary>
        public override string TopRightLabel
        {
            get
            {
                Comp_GalactogenStorageWeapon comp = Pawn?.equipment?.Primary?.GetComp<Comp_GalactogenStorageWeapon>();
                return comp == null ? base.TopRightLabel : comp.SlotCount + "/" + comp.SlotCapacity;
            }
        }
    }

    /// <summary>
    /// 定义化合粘胶弹能力的范围、消耗和弹丸发射参数。
    /// </summary>
    public class CompProperties_AbilityGalactogenGelShot : CompProperties_AbilityEffect
    {
        public int cost = 1;
        public int burstCount = 3;
        public int ticksBetweenShots = 30;
        public float forcedMissRadius = 1.5f;

        /// <summary>
        /// 初始化化合粘胶弹能力效果组件类型。
        /// </summary>
        public CompProperties_AbilityGalactogenGelShot()
        {
            compClass = typeof(CompAbilityEffect_GalactogenGelShot);
        }
    }

    /// <summary>
    /// 在原版能力暖机完成时消耗浓浆，并授权自定义 Job 进入三发射击流程。
    /// </summary>
    public class CompAbilityEffect_GalactogenGelShot : CompAbilityEffect
    {
        private new CompProperties_AbilityGalactogenGelShot Props
        {
            get { return (CompProperties_AbilityGalactogenGelShot)props; }
        }

        /// <summary>
        /// 校验目标与浓浆槽余量，供原版能力目标选择使用。
        /// </summary>
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            Comp_GalactogenStorageWeapon comp = GelStorageComp();
            if (comp == null)
            {
                if (throwMessages)
                {
                    Messages.Message("未装备可发射化合粘胶弹的机炮。", parent.pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            if (!comp.HasEnough(Props.cost))
            {
                if (throwMessages)
                {
                    Messages.Message("机炮浓浆槽不足，无法发射化合粘胶弹。", parent.pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 绘制目标位置的粘胶爆炸半径预览。
        /// </summary>
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            base.DrawEffectPreview(target);
            if (target.Cell.IsValid)
            {
                GenDraw.DrawRadiusRing(target.Cell, ExplosionRadius, Color.white);
            }
        }

        /// <summary>
        /// 消耗一格浓浆，并记录当前 Job 已经通过能力校验进入发射阶段。
        /// </summary>
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Comp_GalactogenStorageWeapon comp = GelStorageComp();
            Job curJob = parent.pawn?.CurJob;
            if (comp != null && comp.TryConsumeForGelShot(parent.pawn, Props.cost) && curJob != null)
            {
                curJob.count = 1;
            }
        }

        /// <summary>
        /// 获取当前主武器上的机炮浓浆储存组件。
        /// </summary>
        private Comp_GalactogenStorageWeapon GelStorageComp()
        {
            return parent.pawn?.equipment?.Primary?.GetComp<Comp_GalactogenStorageWeapon>();
        }

        /// <summary>
        /// 返回本能力配置的一轮发射弹数。
        /// </summary>
        public int BurstCount
        {
            get { return Mathf.Max(1, Props.burstCount); }
        }

        /// <summary>
        /// 返回本能力配置的每发间隔 tick 数。
        /// </summary>
        public int TicksBetweenShots
        {
            get { return Mathf.Max(1, Props.ticksBetweenShots); }
        }

        /// <summary>
        /// 返回本能力配置的每发随机散布半径。
        /// </summary>
        public float ForcedMissRadius
        {
            get { return Mathf.Max(0f, Props.forcedMissRadius); }
        }

        /// <summary>
        /// 返回化合粘胶弹实际落地爆炸半径，供预览与发射逻辑共享。
        /// </summary>
        public float ExplosionRadius
        {
            get { return MunoDefDataRef.Bullet_MunoAC_Gel?.projectile?.explosionRadius ?? 0f; }
        }
    }
}
