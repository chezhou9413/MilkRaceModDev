using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class CompProperties_HeavyArmorEffect : CompProperties
    {
        public HediffDef motionBoostHediff;

        public CompProperties_HeavyArmorEffect()
        {
            compClass = typeof(Comp_HeavyArmorEffect);
        }
    }

    public class Comp_HeavyArmorEffect : ThingComp, IArmorGizmoProvider
    {
        private bool motionBoostEnabled = true;

        private CompProperties_HeavyArmorEffect Props
        {
            get { return (CompProperties_HeavyArmorEffect)props; }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (motionBoostEnabled)
            {
                TryApplyMotionBoost(pawn);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveMotionBoost(pawn);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60))
            {
                return;
            }

            Pawn wearer = (parent as Apparel)?.Wearer;
            if (wearer == null || Props.motionBoostHediff == null)
            {
                return;
            }

            bool hasHediff = wearer.health.hediffSet.GetFirstHediffOfDef(Props.motionBoostHediff) != null;
            if (motionBoostEnabled && !hasHediff)
            {
                TryApplyMotionBoost(wearer);
            }
            else if (!motionBoostEnabled && hasHediff)
            {
                RemoveMotionBoost(wearer);
            }
        }

        public IEnumerable<Gizmo> GetArmorGizmos(Pawn pawn, Comp_GalactogenStorageArmor storageComp)
        {
            if (Props.motionBoostHediff == null)
            {
                yield break;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "激活运动增强系统",
                defaultDesc = "开启后提升移动速度 +10% 与近战闪避率 +10%",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true),
                isActive = delegate { return motionBoostEnabled; },
                toggleAction = delegate
                {
                    motionBoostEnabled = !motionBoostEnabled;
                    if (motionBoostEnabled)
                    {
                        TryApplyMotionBoost(pawn);
                    }
                    else
                    {
                        RemoveMotionBoost(pawn);
                    }
                }
            };
        }

        private void TryApplyMotionBoost(Pawn pawn)
        {
            if (Props.motionBoostHediff == null || pawn.health.hediffSet.GetFirstHediffOfDef(Props.motionBoostHediff) != null)
            {
                return;
            }

            Hediff hediff = pawn.health.AddHediff(Props.motionBoostHediff, pawn.health.hediffSet.GetNotMissingParts().FirstOrFallback(r => r.def == BodyPartDefOf.Torso));
            HediffComp_RemoveIfApparelDropped removeComp = hediff?.TryGetComp<HediffComp_RemoveIfApparelDropped>();
            if (removeComp != null)
            {
                removeComp.wornApparel = (Apparel)parent;
            }
        }

        private void RemoveMotionBoost(Pawn pawn)
        {
            if (Props.motionBoostHediff == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.motionBoostHediff);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref motionBoostEnabled, "motionBoostEnabled", true);
        }
    }
}
