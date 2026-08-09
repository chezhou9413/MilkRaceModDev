using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //统一处理武器、装甲自身浓浆槽与腰带共享备用槽之间的查询、填充和消耗顺序。
    public static class GalactogenStorageUtility
    {
        //返回武器自身槽与所有共享备用槽合计是否足够支付指定消耗。
        public static bool HasEnough(Pawn pawn, Comp_GalactogenStorageWeapon primary, int amount)
        {
            return CountAvailable(pawn, primary?.SlotCount ?? 0, null) >= amount;
        }

        //返回装甲自身槽与所有共享备用槽合计是否足够支付指定消耗。
        public static bool HasEnough(Pawn pawn, Comp_GalactogenStorageArmor primary, int amount)
        {
            return CountAvailable(pawn, primary?.SlotCount ?? 0, primary) >= amount;
        }

        //优先消耗武器自身槽，不足部分再从共享备用槽扣除。
        public static bool TryConsume(Pawn pawn, Comp_GalactogenStorageWeapon primary, int amount, string failureMessage)
        {
            if (!HasEnough(pawn, primary, amount))
            {
                Reject(pawn, failureMessage);
                return false;
            }

            int remaining = amount - (primary?.RemoveSlots(amount) ?? 0);
            ConsumeSharedReserve(pawn, null, remaining);
            return true;
        }

        //优先消耗装甲自身槽，不足部分再从共享备用槽扣除。
        public static bool TryConsume(Pawn pawn, Comp_GalactogenStorageArmor primary, int amount, string failureMessage)
        {
            if (!HasEnough(pawn, primary, amount))
            {
                Reject(pawn, failureMessage);
                return false;
            }

            int remaining = amount - (primary?.RemoveSlots(amount) ?? 0);
            ConsumeSharedReserve(pawn, primary, remaining);
            return true;
        }

        //把自动生产的浓浆优先装入共享储存罐，并返回实际装入数量。
        public static int AddToSharedReserve(Pawn pawn, int amount)
        {
            int remaining = amount;
            foreach (Comp_GalactogenStorageArmor reserve in SharedReserves(pawn))
            {
                remaining -= reserve.AddSlot(remaining);
                if (remaining <= 0)
                {
                    break;
                }
            }

            return amount - remaining;
        }

        //从共享储存罐移除指定数量，用于自动产出事务失败时恢复原状态。
        public static int RemoveFromSharedReserve(Pawn pawn, int amount)
        {
            return ConsumeSharedReserve(pawn, null, amount);
        }

        //按非共享装甲优先、储存罐最后的顺序返回下一件需要装填的穿戴装备。
        public static Comp_GalactogenStorageArmor FindNextRefuelableApparelStorage(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return null;
            }

            Comp_GalactogenStorageArmor reserve = null;
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                Comp_GalactogenStorageArmor comp = apparel.GetComp<Comp_GalactogenStorageArmor>();
                if (comp == null || comp.SlotFull)
                {
                    continue;
                }

                if (!comp.IsSharedReserve)
                {
                    return comp;
                }

                reserve = reserve ?? comp;
            }

            return reserve;
        }

        //计算自身槽与共享备用槽的合计可用数量，并避免把同一储存罐重复计数。
        private static int CountAvailable(Pawn pawn, int primaryCount, Comp_GalactogenStorageArmor excludedReserve)
        {
            int total = primaryCount;
            foreach (Comp_GalactogenStorageArmor reserve in SharedReserves(pawn))
            {
                if (reserve != excludedReserve)
                {
                    total += reserve.SlotCount;
                }
            }

            return total;
        }

        //从共享备用槽中依次扣除指定数量并返回实际扣除量。
        private static int ConsumeSharedReserve(Pawn pawn, Comp_GalactogenStorageArmor excludedReserve, int amount)
        {
            int remaining = amount;
            foreach (Comp_GalactogenStorageArmor reserve in SharedReserves(pawn))
            {
                if (reserve == excludedReserve)
                {
                    continue;
                }

                remaining -= reserve.RemoveSlots(remaining);
                if (remaining <= 0)
                {
                    break;
                }
            }

            return amount - remaining;
        }

        //枚举小人身上所有标记为共享备用槽的储存罐组件。
        private static IEnumerable<Comp_GalactogenStorageArmor> SharedReserves(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                yield break;
            }

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                Comp_GalactogenStorageArmor comp = apparel.GetComp<Comp_GalactogenStorageArmor>();
                if (comp != null && comp.IsSharedReserve)
                {
                    yield return comp;
                }
            }
        }

        //向玩家显示共享槽不足等拒绝原因。
        private static void Reject(Pawn pawn, string message)
        {
            if (!message.NullOrEmpty())
            {
                Messages.Message(message, pawn, MessageTypeDefOf.RejectInput);
            }
        }
    }
}
