using HarmonyLib;
using MunoRaceLib.MunoComp;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MunoRaceLib.Patch
{
    /// <summary>
    /// 负责在小人 Gizmo 流程中维护乳源质装备状态按钮，并在原版武器按钮生成前修复旧存档武器 Verb。
    /// </summary>
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_GalactogenArmor_Patch
    {
        /// <summary>
        /// 在小人 Gizmo 列表后追加乳源质护甲与武器的状态 Gizmo。
        /// </summary>
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            ThingWithComps primary = __instance.equipment?.Primary;
            RefreshWeaponVerbsIfNeeded(primary);

            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            Comp_GalactogenStorageWeapon weaponStorageComp = primary?.GetComp<Comp_GalactogenStorageWeapon>();
            if (weaponStorageComp != null)
            {
                yield return new Gizmo_GalactogenWeaponSlotBar(weaponStorageComp);
                yield return weaponStorageComp.GetGelShotAbility(__instance).GetGizmos().FirstOrDefault();
            }

            if (__instance.apparel == null)
            {
                yield break;
            }

            foreach (Apparel apparel in __instance.apparel.WornApparel)
            {
                Comp_GalactogenStorageArmor storageComp = apparel.GetComp<Comp_GalactogenStorageArmor>();
                if (storageComp != null)
                {
                    foreach (Gizmo gizmo in storageComp.GetStorageGizmos(__instance))
                    {
                        yield return gizmo;
                    }
                }

                foreach (IArmorGizmoProvider provider in apparel.AllComps.OfType<IArmorGizmoProvider>())
                {
                    foreach (Gizmo gizmo in provider.GetArmorGizmos(__instance, storageComp))
                    {
                        yield return gizmo;
                    }
                }
            }
        }

        /// <summary>
        /// 在既有存档武器的已保存 Verb 列表与当前 Def 或运行时上下文不匹配时重建武器 Verb。
        /// </summary>
        private static void RefreshWeaponVerbsIfNeeded(ThingWithComps primary)
        {
            CompEquippable equippable = primary?.GetComp<CompEquippable>();
            if (equippable == null || primary.def.Verbs.NullOrEmpty())
            {
                return;
            }

            List<Verb> verbs = equippable.AllVerbs;
            if (VerbListNeedsReinit(verbs, primary, equippable))
            {
                equippable.VerbTracker.VerbsNeedReinitOnLoad();
                _ = equippable.AllVerbs;
            }
        }

        /// <summary>
        /// 判断武器当前 Verb 列表是否缺少 Def 中声明的 Verb，或含有旧存档遗留的半初始化 Verb。
        /// </summary>
        private static bool VerbListNeedsReinit(List<Verb> verbs, ThingWithComps primary, CompEquippable equippable)
        {
            if (verbs.NullOrEmpty())
            {
                return true;
            }

            int expectedVerbCount = primary.def.Verbs.Count;
            if (!primary.def.tools.NullOrEmpty())
            {
                foreach (Verse.Tool tool in primary.def.tools)
                {
                    if (tool?.Maneuvers == null)
                    {
                        continue;
                    }

                    expectedVerbCount += tool.Maneuvers.Count();
                }
            }

            if (verbs.Count != expectedVerbCount)
            {
                return true;
            }

            foreach (Verb verb in verbs)
            {
                if (verb == null || verb.verbProps == null || verb.caster == null || verb.verbTracker != equippable.VerbTracker)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
