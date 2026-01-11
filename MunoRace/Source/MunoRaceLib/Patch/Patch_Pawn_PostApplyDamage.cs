using HarmonyLib;
using MunoRaceLib.MunoDefRef;
using MunoRaceLib.Tool;
using RimWorld;
using Verse;

namespace MunoRaceLib.Patch
{
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class Patch_Pawn_PostApplyDamage
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            var comp = __instance.GetComp<MunoComp.ThingComp_Galactogen>();
            if (comp == null) return;
            if (totalDamageDealt <= 0f) return;
            if (Rand.Value < 0.5f)
            {
                FilthGalactogenTool.SpawnFilthGalactogen(__instance);
            }
        }
    }
}
