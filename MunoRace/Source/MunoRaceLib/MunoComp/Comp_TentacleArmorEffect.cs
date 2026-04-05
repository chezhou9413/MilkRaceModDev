using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class CompProperties_TentacleArmorEffect : CompProperties
    {
        public HediffDef passiveNerveHediff;
        public HediffDef activeBioLiningHediff;

        public CompProperties_TentacleArmorEffect()
        {
            compClass = typeof(Comp_TentacleArmorEffect);
        }
    }

    public class Comp_TentacleArmorEffect : ThingComp, IArmorGizmoProvider
    {
        private const int ActiveBioLiningDurationTicks = 60000;

        private CompProperties_TentacleArmorEffect Props
        {
            get { return (CompProperties_TentacleArmorEffect)props; }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            EnsureBoundHediff(pawn, Props.passiveNerveHediff, true);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveBoundHediff(pawn, Props.passiveNerveHediff);
            RemoveBoundHediff(pawn, Props.activeBioLiningHediff);
        }

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

        public IEnumerable<Gizmo> GetArmorGizmos(Pawn pawn, Comp_GalactogenStorageArmor storageComp)
        {
            yield return new Command_Action
            {
                defaultLabel = "激活生物内衬",
                defaultDesc = "主动技能：消耗槽内 1 个乳源质浓浆，4 小时内提升利器/钝器防护各 30%，心情 +12，意识 +10%。",
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
