using HarmonyLib;
using MunoRaceLib.MunoComp;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MunoRaceLib.Patch
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_GalactogenArmor_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
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
