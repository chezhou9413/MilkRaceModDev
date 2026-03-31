using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class HediffCompProperties_TentacleArmorMemory : HediffCompProperties
    {
        public ThoughtDef thoughtDef;

        public HediffCompProperties_TentacleArmorMemory()
        {
            compClass = typeof(HediffComp_TentacleArmorMemory);
        }
    }

    public class HediffComp_TentacleArmorMemory : HediffComp
    {
        private HediffCompProperties_TentacleArmorMemory Props => (HediffCompProperties_TentacleArmorMemory)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            TryGainMemory();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (parent.pawn.IsHashIntervalTick(600))
            {
                TryGainMemory();
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (Props.thoughtDef != null && parent.pawn?.needs?.mood?.thoughts?.memories != null)
            {
                parent.pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(Props.thoughtDef);
            }
        }

        private void TryGainMemory()
        {
            if (Props.thoughtDef == null || parent.pawn?.needs?.mood?.thoughts?.memories == null)
            {
                return;
            }

            parent.pawn.needs.mood.thoughts.memories.TryGainMemory(Props.thoughtDef);
        }
    }

    public class HediffCompProperties_ForceEnhancement : HediffCompProperties
    {
        public float minimumSeverity = 4f;

        public HediffCompProperties_ForceEnhancement()
        {
            compClass = typeof(HediffComp_ForceEnhancement);
        }
    }

    public class HediffComp_ForceEnhancement : HediffComp
    {
        private HediffCompProperties_ForceEnhancement Props => (HediffCompProperties_ForceEnhancement)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            EnsureEnhancement();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (parent.pawn.IsHashIntervalTick(600))
            {
                EnsureEnhancement();
            }
        }

        private void EnsureEnhancement()
        {
            if (parent.pawn == null)
            {
                return;
            }

            Hediff enhancement = parent.pawn.health.hediffSet.GetFirstHediffOfDef(MunoDefDataRef.Muno_GalactogenEnhancement);
            if (enhancement == null)
            {
                enhancement = parent.pawn.health.AddHediff(MunoDefDataRef.Muno_GalactogenEnhancement);
            }

            if (enhancement != null && enhancement.Severity < Props.minimumSeverity)
            {
                enhancement.Severity = Props.minimumSeverity;
            }
        }
    }
}
