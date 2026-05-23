using HarmonyLib;
using MunoRaceLib.MunoDefRef;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.Patch
{
    /// <summary>
    /// 负责把原版联络缪诺派系的通讯台行为替换为缪诺专用交换终端窗口。
    /// </summary>
    [HarmonyPatch(typeof(Faction), nameof(Faction.TryOpenComms))]
    public static class Patch_Faction_TryOpenComms_MunoExchange
    {
        /// <summary>
        /// 在通讯对象为缪诺派系时拦截原版对话链，直接打开缪诺交换窗口。
        /// </summary>
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

            Find.WindowStack.Add(new Dialog_MunoHostageExchange(negotiator));
            return false;
        }
    }
}
