using HarmonyLib;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.Patch
{
    /// <summary>
    /// 负责监听和亲公主与玩家殖民者自然结婚的后续事件。
    /// </summary>
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    public static class Patch_Pawn_RelationsTracker_AddDirectRelation_MunoMarriage
    {
        /// <summary>
        /// 在新增配偶关系后判断是否应给予缪诺好感奖励。
        /// </summary>
        public static void Postfix(Pawn_RelationsTracker __instance, PawnRelationDef def, Pawn otherPawn)
        {
            if (def != PawnRelationDefOf.Spouse || __instance == null || otherPawn == null)
            {
                return;
            }

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (MunoMarriageDiplomacyService.IsTrackedPrincess(pawn))
            {
                MunoMarriageDiplomacyService.TryApplyMarriageReward(pawn, otherPawn);
            }
            else if (MunoMarriageDiplomacyService.IsTrackedPrincess(otherPawn))
            {
                MunoMarriageDiplomacyService.TryApplyMarriageReward(otherPawn, pawn);
            }
        }
    }
}
