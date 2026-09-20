using HarmonyLib;
using MunoRaceLib.MunoDefRef;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.Patch
{
    //负责将原版缪诺通讯台接入聚落事务、录像兑换和结局菜单。
    [HarmonyPatch(typeof(Faction), nameof(Faction.TryOpenComms))]
    public static class Patch_Faction_TryOpenComms_MunoExchange
    {
        //在通讯对象为缪诺派系时拦截原版对话链，打开聚落服务菜单。
        public static bool Prefix(Faction __instance, Pawn negotiator)
        {
            if (__instance?.def != MunoDefDataRef.MunoColony_Faction)
            {
                return true;
            }

            if (negotiator?.Map == null)
            {
                Messages.Message("当前没有有效地图，无法联络缪诺派系。", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            MunoScenarios.MunoStoryContacts.Open(negotiator);
            return false;
        }
    }
}
