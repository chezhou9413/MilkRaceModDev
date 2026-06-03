using HarmonyLib;
using MunoRaceLib.MunoWorld;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.Patch
{
    /// <summary>
    /// 负责监听玩家卖出和亲公主，并触发缪诺外交惩罚。
    /// </summary>
    [HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.TryExecute))]
    public static class Patch_TradeDeal_TryExecute_MunoMarriage
    {
        /// <summary>
        /// 在交易执行前记录将被玩家卖出的和亲公主。
        /// </summary>
        public static void Prefix(TradeDeal __instance, ref List<Pawn> __state)
        {
            __state = new List<Pawn>();
            List<Tradeable> tradeables = __instance.AllTradeables;
            for (int i = 0; i < tradeables.Count; i++)
            {
                Tradeable tradeable = tradeables[i];
                if (tradeable.ActionToDo != TradeAction.PlayerSells)
                {
                    continue;
                }

                for (int j = 0; j < tradeable.thingsColony.Count; j++)
                {
                    Pawn pawn = tradeable.thingsColony[j] as Pawn;
                    if (MunoMarriageDiplomacyService.IsTrackedPrincess(pawn))
                    {
                        __state.Add(pawn);
                    }
                }
            }
        }

        /// <summary>
        /// 在交易成功后对被卖出的和亲公主应用后续惩罚。
        /// </summary>
        public static void Postfix(bool __result, bool actuallyTraded, List<Pawn> __state)
        {
            if (!__result || !actuallyTraded || __state == null)
            {
                return;
            }

            for (int i = 0; i < __state.Count; i++)
            {
                MunoMarriageDiplomacyService.TryApplyAbusePenalty(__state[i], "被贩卖给商人");
            }
        }
    }
}
