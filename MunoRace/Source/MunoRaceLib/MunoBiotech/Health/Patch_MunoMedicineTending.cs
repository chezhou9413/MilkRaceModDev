using HarmonyLib;
using Verse;

namespace MunoRaceLib.MunoBiotech.Health
{
    //使药液期间的寄生虫护理额外积累一份治疗进度，不延长包扎时间。
    [HarmonyPatch(typeof(HediffComp_TendDuration), nameof(HediffComp_TendDuration.CompTended))]
    public static class Patch_MunoMedicineTending
    {
        //沿用原版实际治疗质量，使肠道蠕虫与肌肉寄生虫更快达到治愈阈值。
        [HarmonyPostfix]
        public static void Postfix(HediffComp_TendDuration __instance, ref float ___totalTendQuality)
        {
            if ((__instance.Def.defName == "GutWorms" || __instance.Def.defName == "MuscleParasites")
                && __instance.Pawn.health.hediffSet.HasHediff(MunoBiotechDefOf.Muno_MilkMedicine))
                ___totalTendQuality += __instance.tendQuality;
        }
    }
}
