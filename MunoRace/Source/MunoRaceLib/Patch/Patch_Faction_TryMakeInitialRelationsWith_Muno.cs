using HarmonyLib;
using MunoRaceLib.MunoDefRef;
using RimWorld;

namespace MunoRaceLib.Patch
{
    //在原版首次建立势力关系时，把谬诺与普通好感势力的双向初始关系设为负二十。
    [HarmonyPatch(typeof(Faction), nameof(Faction.TryMakeInitialRelationsWith))]
    public static class Patch_Faction_TryMakeInitialRelationsWith_Muno
    {
        //记录调用前是否尚未存在关系，避免后续重复调用覆盖已经发生变化的外交数值。
        [HarmonyPrefix]
        public static void Prefix(Faction __instance, Faction other, out bool __state)
        {
            __state = __instance != null
                && other != null
                && __instance != other
                && __instance.RelationWith(other, allowNull: true) == null;
        }

        //仅处理本次新建且允许好感变化的关系，并保留永久敌对势力的原版状态。
        [HarmonyPostfix]
        public static void Postfix(Faction __instance, Faction other, bool __state)
        {
            if (!__state || __instance == null || other == null)
            {
                return;
            }

            if (__instance.def != MunoDefDataRef.MunoColony_Faction && other.def != MunoDefDataRef.MunoColony_Faction)
            {
                return;
            }

            if (!__instance.HasGoodwill || !other.HasGoodwill
                || __instance.def.PermanentlyHostileTo(other.def)
                || other.def.PermanentlyHostileTo(__instance.def))
            {
                return;
            }

            FactionRelation relation = __instance.RelationWith(other, allowNull: true);
            FactionRelation reverseRelation = other.RelationWith(__instance, allowNull: true);
            if (relation == null || reverseRelation == null)
            {
                return;
            }

            relation.baseGoodwill = -20;
            relation.kind = FactionRelationKind.Neutral;
            reverseRelation.baseGoodwill = -20;
            reverseRelation.kind = FactionRelationKind.Neutral;
        }
    }
}
