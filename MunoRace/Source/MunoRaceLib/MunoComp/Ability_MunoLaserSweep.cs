using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    //使用原版能力系统承载缪诺激光武器的增幅横扫按钮与目标选择。
    public class Ability_MunoLaserSweep : Ability
    {
        private const int SweepCost = 1;

        //供存档反序列化使用的默认构造函数。
        public Ability_MunoLaserSweep()
        {
        }

        //为指定小人创建绑定激光武器浓浆槽的横扫能力。
        public Ability_MunoLaserSweep(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        //检查当前激光武器是否允许横扫。
        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted)
                {
                    return baseReport;
                }

                if (FindSweepVerb() == null)
                {
                    return "未装备可进行激光增幅横扫的武器。";
                }

                Comp_GalactogenStorageWeapon comp = LaserStorageComp();
                if (comp == null)
                {
                    return "未装备带激光增幅浓浆槽的武器。";
                }

                if (!comp.HasEnough(SweepCost))
                {
                    return "激光增幅浓浆槽不足。";
                }

                return true;
            }
        }

        //创建横扫 Job，并指定真正发射 Beam 的武器副 Verb。
        public override Job GetJob(LocalTargetInfo target, LocalTargetInfo destination)
        {
            Verb_MunoLaserSweep sweepVerb = FindSweepVerb();
            if (sweepVerb == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_MunoLaserSweep, target, destination);
            job.ability = this;
            job.verbToUse = sweepVerb;
            return job;
        }

        //获取当前主武器上的激光增幅浓浆槽。
        private Comp_GalactogenStorageWeapon LaserStorageComp()
        {
            return pawn?.equipment?.Primary?.GetComp<Comp_GalactogenStorageWeapon>();
        }

        //查找当前主武器上负责横扫的 Beam Verb。
        private Verb_MunoLaserSweep FindSweepVerb()
        {
            CompEquippable equippable = pawn?.equipment?.Primary?.GetComp<CompEquippable>();
            if (equippable == null)
            {
                return null;
            }

            for (int i = 0; i < equippable.AllVerbs.Count; i++)
            {
                if (equippable.AllVerbs[i] is Verb_MunoLaserSweep sweepVerb)
                {
                    return sweepVerb;
                }
            }

            return null;
        }
    }

    //显示激光增幅横扫能力按钮，并在右上角标出浓浆槽余量。
    public class Command_AbilityMunoLaserSweep : Command_Ability
    {
        //绑定原版能力命令所需的能力对象与小人。
        public Command_AbilityMunoLaserSweep(Ability ability, Pawn pawn) : base(ability, pawn)
        {
        }

        //返回当前激光增幅浓浆槽数量，显示在能力按钮右上角。
        public override string TopRightLabel
        {
            get
            {
                Comp_GalactogenStorageWeapon comp = Pawn?.equipment?.Primary?.GetComp<Comp_GalactogenStorageWeapon>();
                return comp == null ? base.TopRightLabel : comp.SlotCount + "/" + comp.SlotCapacity;
            }
        }
    }

    //校验激光增幅横扫的目标与浓浆槽余量。
    public class CompProperties_AbilityMunoLaserSweep : CompProperties_AbilityEffect
    {
        public int cost = 1;

        //初始化激光增幅横扫能力效果组件类型。
        public CompProperties_AbilityMunoLaserSweep()
        {
            compClass = typeof(CompAbilityEffect_MunoLaserSweep);
        }
    }

    //在目标选择阶段校验浓浆槽，实际消耗仍由武器 Beam Verb 在暖机完成时处理。
    public class CompAbilityEffect_MunoLaserSweep : CompAbilityEffect
    {
        private new CompProperties_AbilityMunoLaserSweep Props
        {
            get { return (CompProperties_AbilityMunoLaserSweep)props; }
        }

        //校验目标与浓浆槽余量，供原版能力目标选择使用。
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            Comp_GalactogenStorageWeapon comp = parent.pawn?.equipment?.Primary?.GetComp<Comp_GalactogenStorageWeapon>();
            if (comp == null)
            {
                if (throwMessages)
                {
                    Messages.Message("未装备带激光增幅浓浆槽的武器。", parent.pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            if (!comp.HasEnough(Props.cost))
            {
                if (throwMessages)
                {
                    Messages.Message("激光增幅浓浆槽不足，无法横扫。", parent.pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            return true;
        }
    }
}
