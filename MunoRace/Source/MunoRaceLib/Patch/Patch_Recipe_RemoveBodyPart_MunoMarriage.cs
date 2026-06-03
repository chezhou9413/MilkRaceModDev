using HarmonyLib;
using MunoRaceLib.MunoWorld;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.Patch
{
    /// <summary>
    /// 负责监听玩家对和亲公主摘取器官致死的行为。
    /// </summary>
    [HarmonyPatch(typeof(Recipe_RemoveBodyPart), nameof(Recipe_RemoveBodyPart.ApplyOnPawn))]
    public static class Patch_Recipe_RemoveBodyPart_MunoMarriage
    {
        /// <summary>
        /// 在手术前记录本次手术是否属于摘取自然器官。
        /// </summary>
        public static void Prefix(Pawn pawn, BodyPartRecord part, ref bool __state)
        {
            __state = MunoMarriageDiplomacyService.IsTrackedPrincess(pawn)
                && part != null
                && HealthUtility.PartRemovalIntent(pawn, part) == BodyPartRemovalIntent.Harvest;
        }

        /// <summary>
        /// 在手术后如果公主死亡则应用虐待惩罚。
        /// </summary>
        public static void Postfix(Pawn pawn, bool __state)
        {
            if (__state && pawn != null && pawn.Dead)
            {
                MunoMarriageDiplomacyService.TryApplyAbusePenalty(pawn, "死于器官摘取");
            }
        }
    }
}
