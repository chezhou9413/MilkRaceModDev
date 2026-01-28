using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class CompUseEffect_UpgradeGalactogen : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            if (usedBy.def.defName != "MunoRace")
            {
                Messages.Message("只有缪诺族可以使用此药水。", usedBy, MessageTypeDefOf.RejectInput, false);
                return;
            }

            Hediff hediff = usedBy.health.hediffSet.GetFirstHediffOfDef(MunoDefDataRef.Muno_GalactogenEnhancement);
            if (hediff != null && hediff.Severity >= 4.0f)
            {
                Messages.Message("乳源质增幅已达到最大阶段。", usedBy, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (hediff == null)
            {
                //如果没有 Hediff，添加并设置为初始阶段
                hediff = usedBy.health.AddHediff(MunoDefDataRef.Muno_GalactogenEnhancement);
                hediff.Severity = 0.1f;
            }
            else
            {
                // 如果有 Hediff，增加到下一个整数阶段
                float currentSeverity = hediff.Severity;
                float nextSeverity = 0f;
                if (currentSeverity < 1.0f) nextSeverity = 1.0f;
                else if (currentSeverity < 2.0f) nextSeverity = 2.0f;
                else if (currentSeverity < 3.0f) nextSeverity = 3.0f;
                else if (currentSeverity < 4.0f) nextSeverity = 4.0f;

                hediff.Severity = nextSeverity;
            }

            Messages.Message("乳源质能力已强化！", usedBy, MessageTypeDefOf.PositiveEvent, false);

            // Manually destroy the item only on success
            this.parent.Destroy(DestroyMode.Vanish);
        }
    }
}