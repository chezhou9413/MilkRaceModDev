using HarmonyLib;
using MunoRaceLib.MunoWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.Patch
{
    /// <summary>
    /// 在玩家远行队停驻缪诺据点时追加成员交换按钮。
    /// </summary>
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetCaravanGizmos))]
    public static class Patch_Settlement_GetCaravanGizmos_MunoExchange
    {
        /// <summary>
        /// 在原版据点远行队 Gizmo 末尾追加缪诺成员交换按钮。
        /// </summary>
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
                defaultLabel = "上交换取缪诺成员",
                defaultDesc = "向当前缪诺据点上交一名远行队成员，并换取一名新的缪诺成员直接加入远行队。",
                icon = Settlement.ShowSellableItemsCommand,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_MunoHostageExchange(__instance, caravan));
                }
            };
        }
    }
}
