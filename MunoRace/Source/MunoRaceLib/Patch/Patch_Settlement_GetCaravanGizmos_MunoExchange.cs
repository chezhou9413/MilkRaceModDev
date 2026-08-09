using HarmonyLib;
using MunoRaceLib.MunoWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.Patch
{
    //在玩家远行队停驻缪诺据点时追加缪诺人口交换按钮。
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetCaravanGizmos))]
    public static class Patch_Settlement_GetCaravanGizmos_MunoExchange
    {
        //在原版据点远行队 Gizmo 末尾追加缪诺人口交换按钮。
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Settlement __instance, Caravan caravan)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            if (!MunoHostageExchangeService.CanExchangeAt(__instance, caravan))
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "缪诺人口交换",
                defaultDesc = "向当前缪诺据点上交多名远行队成员，并逐人选择缪诺成员或原版任务式等值物资奖励。",
                icon = Settlement.ShowSellableItemsCommand,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_MunoHostageExchange(__instance, caravan));
                }
            };
        }
    }
}
