using HarmonyLib;
using MunoRaceLib.MunoWorld;
using RimWorld;

namespace MunoRaceLib.MunoEndings
{
    //在接收穿梭机实际离开地图时结算人口交付，避免起飞动画开始就达成结局。
    [HarmonyPatch(typeof(FlyShipLeaving), "LeaveMap")]
    public static class Patch_FlyShipLeaving_MunoDelivery
    {
        //核实这架离场飞船包含当前接收会话的全部乘员。
        [HarmonyPrefix]
        public static void Prefix(FlyShipLeaving __instance, out bool __state)
        {
            __state = MunoShuttleExchangeService.CurrentSession().PrepareDeparture(__instance);
        }

        //在原版完成离场后发放奖励并记录实际交付时刻。
        [HarmonyPostfix]
        public static void Postfix(bool __state)
        {
            if (__state) MunoShuttleExchangeService.CurrentSession().CompleteDeparture();
        }
    }
}
