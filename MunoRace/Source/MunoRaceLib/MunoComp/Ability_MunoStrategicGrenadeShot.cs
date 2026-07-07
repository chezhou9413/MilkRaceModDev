using MunoRaceLib.MunoDefRef;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    //使用原版能力系统承载缪诺战略步枪的榴弹发射按钮与目标选择。
    public class Ability_MunoStrategicGrenadeShot : Ability
    {
        //供存档反序列化使用的默认构造函数。
        public Ability_MunoStrategicGrenadeShot()
        {
        }

        //为指定小人创建绑定战略步枪榴弹仓的能力。
        public Ability_MunoStrategicGrenadeShot(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        //检查当前战略步枪榴弹仓是否允许发射。
        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted)
                {
                    return baseReport;
                }

                Comp_StrategicGrenadeStorageWeapon comp = GrenadeStorageComp();
                if (comp == null)
                {
                    return "未装备可发射战略榴弹的缪诺战略步枪。";
                }

                if (!comp.HasAmmo())
                {
                    return "战略步枪榴弹仓为空。";
                }

                return true;
            }
        }

        //获取当前主武器上的战略榴弹仓组件。
        private Comp_StrategicGrenadeStorageWeapon GrenadeStorageComp()
        {
            return pawn?.equipment?.Primary?.GetComp<Comp_StrategicGrenadeStorageWeapon>();
        }

        //创建自定义发射 Job，让能力暖机后发射战略榴弹弹丸。
        public override Job GetJob(LocalTargetInfo target, LocalTargetInfo destination)
        {
            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_MunoStrategicGrenadeShot, target, destination);
            job.ability = this;
            job.verbToUse = verb;
            return job;
        }
    }

    //显示战略榴弹能力按钮，并在右上角标出榴弹仓余量。
    public class Command_AbilityMunoStrategicGrenadeShot : Command_Ability
    {
        //绑定原版能力命令所需的能力对象与小人。
        public Command_AbilityMunoStrategicGrenadeShot(Ability ability, Pawn pawn) : base(ability, pawn)
        {
        }

        //返回当前战略榴弹仓数量，显示在能力按钮右上角。
        public override string TopRightLabel
        {
            get
            {
                Comp_StrategicGrenadeStorageWeapon comp = Pawn?.equipment?.Primary?.GetComp<Comp_StrategicGrenadeStorageWeapon>();
                return comp == null ? base.TopRightLabel : comp.AmmoCount + "/" + comp.AmmoCapacity;
            }
        }
    }

    //定义战略榴弹能力的弹丸散布参数。
    public class CompProperties_AbilityMunoStrategicGrenadeShot : CompProperties_AbilityEffect
    {
        public int cost = 1;
        public float forcedMissRadius = 1.2f;

        //初始化战略榴弹能力效果组件类型。
        public CompProperties_AbilityMunoStrategicGrenadeShot()
        {
            compClass = typeof(CompAbilityEffect_MunoStrategicGrenadeShot);
        }
    }

    //在原版能力暖机完成时消耗榴弹仓，并授权自定义 Job 发射弹丸。
    public class CompAbilityEffect_MunoStrategicGrenadeShot : CompAbilityEffect
    {
        private new CompProperties_AbilityMunoStrategicGrenadeShot Props
        {
            get { return (CompProperties_AbilityMunoStrategicGrenadeShot)props; }
        }

        //校验目标与榴弹仓余量，供原版能力目标选择使用。
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            Comp_StrategicGrenadeStorageWeapon comp = GrenadeStorageComp();
            if (comp == null)
            {
                if (throwMessages)
                {
                    Messages.Message("未装备可发射战略榴弹的缪诺战略步枪。", parent.pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            if (!comp.HasAmmo())
            {
                if (throwMessages)
                {
                    Messages.Message("战略步枪榴弹仓为空。", parent.pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            return true;
        }

        //绘制战略榴弹爆炸半径预览。
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            base.DrawEffectPreview(target);
            if (target.Cell.IsValid)
            {
                GenDraw.DrawRadiusRing(target.Cell, ExplosionRadius, Color.white);
            }
        }

        //消耗一发战略榴弹，并记录当前 Job 已经通过能力校验进入发射阶段。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Comp_StrategicGrenadeStorageWeapon comp = GrenadeStorageComp();
            Job curJob = parent.pawn?.CurJob;
            if (comp != null && comp.TryConsumeGrenade(parent.pawn) && curJob != null)
            {
                curJob.count = Props.cost;
            }
        }

        //获取当前主武器上的战略榴弹仓组件。
        private Comp_StrategicGrenadeStorageWeapon GrenadeStorageComp()
        {
            return parent.pawn?.equipment?.Primary?.GetComp<Comp_StrategicGrenadeStorageWeapon>();
        }

        //返回战略榴弹随机散布半径。
        public float ForcedMissRadius
        {
            get { return Mathf.Max(0f, Props.forcedMissRadius); }
        }

        //返回战略榴弹爆炸半径，供预览与发射逻辑共享。
        public float ExplosionRadius
        {
            get { return MunoDefDataRef.Bullet_MunoSR_Grenade?.projectile?.explosionRadius ?? 0f; }
        }
    }
}
