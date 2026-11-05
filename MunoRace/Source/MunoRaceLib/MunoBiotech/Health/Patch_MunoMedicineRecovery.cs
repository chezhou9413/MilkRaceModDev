using HarmonyLib;
using Verse;

namespace MunoRaceLib.MunoBiotech.Health
{
    //加快免疫型疾病已经开始的恢复，不放大患病时的恶化速度。
    [HarmonyPatch(typeof(HediffComp_Immunizable), nameof(HediffComp_Immunizable.SeverityChangePerDay))]
    public static class Patch_MunoMedicineRecovery
    {
        //只提高负向严重度变化，免疫增长速度由药液的状态属性负责。
        [HarmonyPostfix]
        public static void Postfix(HediffComp_Immunizable __instance, ref float __result)
        {
            if (__result < 0f && __instance.Pawn.health.hediffSet.HasHediff(MunoBiotechDefOf.Muno_MilkMedicine))
                __result *= 1.5f;
        }
    }
}
