using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class CompProperties_TargetEffect_MunoHealGun : CompProperties
    {
        public int maxWoundsPerShot = 3;
        public float tendQuality = 0.8f;
        public float maxTendQuality = 1f;

        public CompProperties_TargetEffect_MunoHealGun()
        {
            compClass = typeof(CompTargetEffect_MunoHealGun);
        }
    }

    public class CompTargetEffect_MunoHealGun : CompTargetEffect
    {
        private CompProperties_TargetEffect_MunoHealGun PropsHealGun => (CompProperties_TargetEffect_MunoHealGun)props;

        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!(target is Pawn pawn))
            {
                return;
            }

            List<Hediff> wounds = GetBleedingWounds(pawn);
            int tendedCount = 0;
            int woundsToTreat = Mathf.Min(PropsHealGun.maxWoundsPerShot, wounds.Count);

            for (int i = 0; i < woundsToTreat; i++)
            {
                wounds[i].Tended(PropsHealGun.tendQuality, PropsHealGun.maxTendQuality, i + 1);
                tendedCount++;
            }

            if (tendedCount > 0)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "NumWoundsTended".Translate(tendedCount), 3.65f);
            }
        }

        public override bool CanApplyOn(Thing target)
        {
            return target is Pawn pawn && GetBleedingWounds(pawn).Count > 0;
        }

        private static List<Hediff> GetBleedingWounds(Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs
                .Where(hediff => (hediff is Hediff_Injury || hediff is Hediff_MissingPart) && hediff.TendableNow() && hediff.BleedRate > 0f)
                .OrderByDescending(hediff => hediff.BleedRate)
                .ThenByDescending(hediff => hediff.Severity)
                .ToList();
        }
    }
}
