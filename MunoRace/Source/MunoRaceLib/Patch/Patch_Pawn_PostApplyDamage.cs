using HarmonyLib;
using MunoRaceLib.Tool;
using Verse;

namespace MunoRaceLib.Patch
{
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    //负责在含乳液组件的 Pawn 受到有效伤害后按概率生成乳液污物。
    public static class Patch_Pawn_PostApplyDamage
    {
        //在伤害结算完成后检查乳液组件、伤害量和触发概率。
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
