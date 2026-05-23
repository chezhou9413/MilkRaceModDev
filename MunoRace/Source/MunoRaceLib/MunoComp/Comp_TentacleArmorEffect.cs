using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    /// <summary>
    /// 负责定义触手动力甲提供的被动神经强化与主动生物内衬效果参数。
    /// </summary>
    public class CompProperties_TentacleArmorEffect : CompProperties
    {
        public HediffDef passiveNerveHediff;
        public HediffDef activeBioLiningHediff;

        /// <summary>
        /// 初始化触手动力甲效果组件的绑定类型。
        /// </summary>
        public CompProperties_TentacleArmorEffect()
        {
            compClass = typeof(Comp_TentacleArmorEffect);
        }
    }

    /// <summary>
    /// 负责在触手动力甲穿脱与穿戴期间维护 Hediff，并提供对应的操作 Gizmo。
    /// </summary>
    public class Comp_TentacleArmorEffect : ThingComp, IArmorGizmoProvider
    {
        private const int ActiveBioLiningDurationTicks = 60000;

        private CompProperties_TentacleArmorEffect Props
        {
            get { return (CompProperties_TentacleArmorEffect)props; }
        }

        /// <summary>
        /// 在装备穿上时补上被动神经强化效果。
        /// </summary>
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            EnsureBoundHediff(pawn, Props.passiveNerveHediff, true);
        }

        /// <summary>
        /// 在装备脱下时移除由装备提供的相关 Hediff。
        /// </summary>
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveBoundHediff(pawn, Props.passiveNerveHediff);
            RemoveBoundHediff(pawn, Props.activeBioLiningHediff);
        }

        /// <summary>
        /// 在穿戴期间持续校正被动与主动效果，避免读档或异常状态导致丢失。
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60))
            {
                return;
            }

            Pawn wearer = (parent as Apparel)?.Wearer;
            if (wearer != null)
            {
                EnsureBoundHediff(wearer, Props.passiveNerveHediff, true);
                NormalizeActiveBioLining(wearer);
            }
        }

        /// <summary>
        /// 为触手动力甲生成主动技能按钮，并在效果生效时显示剩余时间。
        /// </summary>
        public IEnumerable<Gizmo> GetArmorGizmos(Pawn pawn, Comp_GalactogenStorageArmor storageComp)
        {
            string label = TentacleArmorGizmoUtility.BuildTimedLabel(pawn, Props.activeBioLiningHediff, "激活生物内衬");
            string desc = "主动技能：消耗槽内 1 个乳源质浓浆，4 小时内提升利器/钝器防护各 30%，心情 +12，意识 +10%。";
            string remainingTime = TentacleArmorGizmoUtility.GetRemainingTimeText(pawn, Props.activeBioLiningHediff);
            if (!remainingTime.NullOrEmpty())
            {
                desc += "\n当前剩余时间: " + remainingTime;
            }

            yield return new Command_Action
            {
                defaultLabel = label,
                defaultDesc = desc,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true),
                Disabled = storageComp == null || !storageComp.HasEnough(1) || pawn.Downed,
                disabledReason = pawn.Downed ? "小人已倒下" : (storageComp != null && storageComp.HasEnough(1) ? string.Empty : "装甲浓浆槽不足"),
                action = delegate
                {
                    if (storageComp != null && storageComp.TryConsumeForAbility(pawn, 1))
                    {
                        ActivateTimedHediff(pawn, Props.activeBioLiningHediff, ActiveBioLiningDurationTicks);
                    }
                }
            };

        }

        /// <summary>
        /// 为指定 Hediff 创建或续期限时生效的生物内衬效果。
        /// </summary>
        private void ActivateTimedHediff(Pawn pawn, HediffDef hediffDef, int durationTicks)
        {
            Hediff hediff = EnsureBoundHediff(pawn, hediffDef, false);
            if (hediff == null)
            {
                return;
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = durationTicks;
            }
        }

        /// <summary>
        /// 修正主动效果的持续时间，防止异常叠加出超出预期的时长。
        /// </summary>
        private void NormalizeActiveBioLining(Pawn pawn)
        {
            if (Props.activeBioLiningHediff == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.activeBioLiningHediff);
            if (hediff == null)
            {
                return;
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears == null)
            {
                pawn.health.RemoveHediff(hediff);
                return;
            }

            if (disappears.ticksToDisappear > ActiveBioLiningDurationTicks)
            {
                disappears.ticksToDisappear = ActiveBioLiningDurationTicks;
            }
        }

        /// <summary>
        /// 确保目标小人拥有指定 Hediff，并在需要时绑定当前装备以支持随脱卸移除。
        /// </summary>
        private Hediff EnsureBoundHediff(Pawn pawn, HediffDef hediffDef, bool bindToApparel)
        {
            if (hediffDef == null)
            {
                return null;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = pawn.health.AddHediff(hediffDef, pawn.health.hediffSet.GetNotMissingParts().FirstOrFallback(r => r.def == BodyPartDefOf.Torso));
            }

            if (bindToApparel)
            {
                HediffComp_RemoveIfApparelDropped removeComp = hediff?.TryGetComp<HediffComp_RemoveIfApparelDropped>();
                if (removeComp != null)
                {
                    removeComp.wornApparel = (Apparel)parent;
                }
            }

            return hediff;
        }

        /// <summary>
        /// 从目标小人身上移除指定 Hediff。
        /// </summary>
        private void RemoveBoundHediff(Pawn pawn, HediffDef hediffDef)
        {
            if (hediffDef == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}
