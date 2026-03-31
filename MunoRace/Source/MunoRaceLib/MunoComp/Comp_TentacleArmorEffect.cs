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
        public ThingDef transformedArmorDef;

        public CompProperties_TentacleArmorEffect()
        {
            compClass = typeof(Comp_TentacleArmorEffect);
        }
    }

    public class Comp_TentacleArmorEffect : ThingComp, IArmorGizmoProvider
    {
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
            }
        }

        public IEnumerable<Gizmo> GetArmorGizmos(Pawn pawn, Comp_GalactogenStorageArmor storageComp)
        {
            yield return new Command_Action
            {
                defaultLabel = "激活生物内衬",
                defaultDesc = "消耗槽内 1 个乳源质浓浆，4 小时内提升利器/钝器防护各 30%，心情 +12，意识 +10%",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true),
                Disabled = storageComp == null || !storageComp.HasEnough(1) || pawn.Downed,
                disabledReason = pawn.Downed ? "小人已倒下" : (storageComp != null && storageComp.HasEnough(1) ? string.Empty : "装甲浓浆槽不足"),
                action = delegate
                {
                    if (storageComp != null && storageComp.TryConsumeForAbility(pawn, 1))
                    {
                        ActivateTimedHediff(pawn, Props.activeBioLiningHediff, 60000);
                    }
                }
            };

            if (Props.transformedArmorDef != null)
            {
                yield return new Command_Action
                {
                    defaultLabel = "转化为失控触手甲",
                    defaultDesc = "将当前触手甲转化为失控触手甲道具。",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true),
                    action = TransformArmor
                };
            }
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

        private void TransformArmor()
        {
            Apparel apparel = parent as Apparel;
            Pawn wearer = apparel?.Wearer;
            if (apparel == null || Props.transformedArmorDef == null)
            {
                return;
            }

            Map map = apparel.MapHeld;
            IntVec3 pos = apparel.PositionHeld;

            if (wearer != null)
            {
                wearer.apparel.Remove(apparel);
            }

            Thing newThing = ThingMaker.MakeThing(Props.transformedArmorDef, apparel.Stuff);
            newThing.HitPoints = Mathf.Clamp(apparel.HitPoints, 1, newThing.MaxHitPoints);
            CompQuality oldQuality = apparel.TryGetComp<CompQuality>();
            CompQuality newQuality = newThing.TryGetComp<CompQuality>();
            if (oldQuality != null && newQuality != null)
            {
                newQuality.SetQuality(oldQuality.Quality, ArtGenerationContext.Colony);
            }

            Comp_GalactogenStorageArmor oldStorage = apparel.GetComp<Comp_GalactogenStorageArmor>();
            Comp_GalactogenStorageArmor newStorage = newThing.TryGetComp<Comp_GalactogenStorageArmor>();
            if (oldStorage != null && newStorage != null && oldStorage.SlotCount > 0)
            {
                newStorage.AddSlot(oldStorage.SlotCount);
            }

            if (wearer != null)
            {
                wearer.apparel.Wear((Apparel)newThing, false);
            }
            else if (map != null)
            {
                GenPlace.TryPlaceThing(newThing, pos, map, ThingPlaceMode.Near);
            }

            apparel.Destroy();
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
