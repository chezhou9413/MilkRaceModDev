using MunoRaceLib.MunoDefRef;
using RimWorld;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class CompUseEffect_TentacleArmorDisabler : CompUseEffect
    {
        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (p?.apparel == null)
            {
                return "必须由穿着触手动力甲的角色使用。";
            }

            Apparel armor = FindTentacleArmor(p);
            if (armor == null)
            {
                return "需要先穿着触手动力甲。";
            }

            return true;
        }

        public override void DoEffect(Pawn usedBy)
        {
            Apparel armor = FindTentacleArmor(usedBy);
            if (armor == null)
            {
                Messages.Message("需要先穿着触手动力甲。", usedBy, MessageTypeDefOf.RejectInput, false);
                return;
            }

            TransformArmor(usedBy, armor);
            Messages.Message("触手动力甲已解除限制，转化为狂乱触手甲。", usedBy, MessageTypeDefOf.PositiveEvent, false);
            parent.Destroy(DestroyMode.Vanish);
        }

        private Apparel FindTentacleArmor(Pawn pawn)
        {
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel.def == MunoDefDataRef.MunoRace_Apparel_Tentaclearmor)
                {
                    return apparel;
                }
            }

            return null;
        }

        private void TransformArmor(Pawn wearer, Apparel apparel)
        {
            Thing newThing = ThingMaker.MakeThing(MunoDefDataRef.MunoRace_Apparel_Tentaclefrenzyarmor, apparel.Stuff);
            Thing headThing = ThingMaker.MakeThing(MunoDefDataRef.MunoRace_Apparel_TentaclefrenzyarmorHead, apparel.Stuff);
            newThing.HitPoints = Mathf.Clamp(apparel.HitPoints, 1, newThing.MaxHitPoints);
            headThing.HitPoints = Mathf.Clamp(apparel.HitPoints, 1, headThing.MaxHitPoints);

            CompQuality oldQuality = apparel.TryGetComp<CompQuality>();
            CompQuality newQuality = newThing.TryGetComp<CompQuality>();
            if (oldQuality != null && newQuality != null)
            {
                newQuality.SetQuality(oldQuality.Quality, ArtGenerationContext.Colony);
            }

            CompQuality newHeadQuality = headThing.TryGetComp<CompQuality>();
            if (oldQuality != null && newHeadQuality != null)
            {
                newHeadQuality.SetQuality(oldQuality.Quality, ArtGenerationContext.Colony);
            }

            Comp_GalactogenStorageArmor oldStorage = apparel.GetComp<Comp_GalactogenStorageArmor>();
            Comp_GalactogenStorageArmor newStorage = newThing.TryGetComp<Comp_GalactogenStorageArmor>();
            if (oldStorage != null && newStorage != null && oldStorage.SlotCount > 0)
            {
                newStorage.AddSlot(oldStorage.SlotCount);
            }

            wearer.apparel.Remove(apparel);
            wearer.apparel.Wear((Apparel)newThing, false, true);
            wearer.apparel.Wear((Apparel)headThing, true, true);
            apparel.Destroy();
        }
    }
}
