using HarmonyLib;
using MunoRaceLib.MunoComp;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MunoRaceLib.Patch
{
    //负责在小人 Gizmo 流程中维护乳源质装备状态按钮与战略榴弹仓按钮。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_GalactogenArmor_Patch
    {
        //在小人 Gizmo 列表后追加乳源质护甲、武器浓浆槽与战略榴弹仓状态 Gizmo。
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            ThingWithComps primary = __instance.equipment?.Primary;

            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            Comp_GalactogenStorageWeapon weaponStorageComp = primary?.GetComp<Comp_GalactogenStorageWeapon>();
            if (weaponStorageComp != null)
            {
                foreach (Gizmo gizmo in weaponStorageComp.GetStorageGizmos(__instance))
                {
                    yield return gizmo;
                }
            }

            Comp_StrategicGrenadeStorageWeapon strategicGrenadeComp = primary?.GetComp<Comp_StrategicGrenadeStorageWeapon>();
            if (strategicGrenadeComp != null)
            {
                foreach (Gizmo gizmo in strategicGrenadeComp.GetStorageGizmos(__instance))
                {
                    yield return gizmo;
                }
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
    }
}
