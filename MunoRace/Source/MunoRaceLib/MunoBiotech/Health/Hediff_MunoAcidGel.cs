using System.Linq;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoBiotech.Health
{
    //让附着酸液逐步腐蚀随机外部部位，在一小时内达到部位生命值九成的上限。
    public class Hediff_MunoAcidGel : Hediff_MunoCoating
    {
        private int pulseTicks;

        //按固定间隔结算腐蚀，连续喷射仅刷新附着时间，不重置腐蚀增长。
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.Dead)
                return;
            pulseTicks += delta;
            if (pulseTicks < 250)
                return;
            pulseTicks %= 250;
            CorrodePart();
        }

        //限制酸蚀伤口总量及部位剩余生命，避免同一涂层不断累加至断肢。
        private void CorrodePart()
        {
            if (!pawn.health.hediffSet.GetNotMissingParts()
                .Where(part => part.depth == BodyPartDepth.Outside && part.coverageAbs > 0f
                    && pawn.health.hediffSet.GetPartHealth(part) > part.def.GetMaxHealth(pawn) * 0.1f)
                .TryRandomElement(out BodyPartRecord part))
                return;

            float maxHealth = part.def.GetMaxHealth(pawn);
            float existingDamage = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff.def == MunoBiotechDefOf.Muno_AcidBurn && hediff.Part == part)
                .Sum(hediff => hediff.Severity);
            float ceiling = maxHealth * 0.9f * Mathf.Clamp01(ageTicks / 2500f);
            float amount = Mathf.Min(ceiling - existingDamage,
                pawn.health.hediffSet.GetPartHealth(part) - maxHealth * 0.1f);
            if (amount <= 0f)
                return;

            //酸液已经附着在身体上，直接结算组织损伤，避免承伤倍率突破九成上限。
            Hediff injury = HediffMaker.MakeHediff(MunoBiotechDefOf.Muno_AcidBurn, pawn, part);
            injury.Severity = amount;
            injury.sourceHediffDef = def;
            pawn.health.AddHediff(injury);
        }

        //保存两次腐蚀之间已经经过的时间。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pulseTicks, "pulseTicks");
        }
    }
}
