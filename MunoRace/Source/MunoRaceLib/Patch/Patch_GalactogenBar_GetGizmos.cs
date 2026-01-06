using HarmonyLib;
using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoGizmo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MunoRaceLib.Patch
{
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("GetGizmos")]
    public static class Patch_GalactogenBar_GetGizmos
    {
        static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance != null && Find.Selector.NumSelected < 2)
            {
                if (__instance.TryGetComp<ThingComp_Galactogen>() != null)
                {
                    var list = new List<Gizmo>(__result);
                    if (__instance.IsColonistPlayerControlled)
                    {
                        list.Add(new Gizmo_GalactogenBar(__instance));
                    }
                    __result = list;
                }
            }
        }
    }
}
