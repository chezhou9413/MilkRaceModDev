using System.Collections.Generic;
using System.Linq;
using MunoRaceLib.MunoScenarios;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoRecording
{
    //通过聚落通讯消费实体录像带，并按独立奖励池投放随机物资。
    public static class MunoRecordingExchange
    {
        //显示可提交的录像数量，并在操作时重新检查地图和库存。
        public static void Open(Pawn negotiator)
        {
            if (!CanContact(negotiator)) return;
            int total = Tapes(negotiator.Map).Sum(t => t.stackCount);
            if (total == 0)
            {
                Messages.Message("当前地图没有可上交的录像带，请先拍摄并解除物品禁用。", MessageTypeDefOf.RejectInput, false);
                return;
            }
            var options = new List<FloatMenuOption>();
            foreach (int amount in new[] { 1, 5, 20, System.Math.Min(total, 50) }.Where(n => n <= total).Distinct().OrderBy(n => n))
            {
                int count = amount;
                options.Add(new FloatMenuOption("上交 " + count + " 份录像带，领取 " + count + " 份随机奖励", () => Exchange(negotiator, count)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        //检查操作者是否能够与仍存在的非敌对聚落进行通讯。
        private static bool CanContact(Pawn pawn)
        {
            Faction faction = MunoStoryUtility.Faction;
            if (pawn?.Map == null || !pawn.IsColonistPlayerControlled || pawn.Downed
                || faction == null || faction.defeated || faction.deactivated || faction.HostileTo(Faction.OfPlayer))
            {
                Messages.Message("需要可行动的殖民者，以及仍存在且不敌对的缪诺聚落。", MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }

        //收集地图中已显露且未禁用的实体录像带。
        private static List<Thing> Tapes(Map map)
        {
            return map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Muno_RecordingTape"))
                .Where(t => t.Spawned && !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map)).ToList();
        }

        //重新核对库存，成功投放奖励后精确扣除本次数量，避免重复领取。
        private static void Exchange(Pawn negotiator, int count)
        {
            if (!CanContact(negotiator)) return;
            List<Thing> tapes = Tapes(negotiator.Map);
            if (tapes.Sum(t => t.stackCount) < count)
            {
                Messages.Message("录像带库存已经变化，请重新选择上交数量。", MessageTypeDefOf.RejectInput, false);
                return;
            }
            var rewards = new List<Thing>();
            for (int i = 0; i < count; i++)
            {
                MunoRecordingRewardDef reward = MunoStoryConfigDef.Current.recordingRewards.RandomElementByWeight(r => r.weight);
                int amount = reward.count.RandomInRange;
                while (amount > 0)
                {
                    Thing thing = ThingMaker.MakeThing(reward.thingDef);
                    thing.stackCount = System.Math.Min(amount, thing.def.stackLimit);
                    amount -= thing.stackCount;
                    rewards.Add(thing);
                }
            }
            if (!MunoExchangeRewardService.TryDeliverRewardsToMap(negotiator.Map, new List<Pawn>(), rewards, out string reason))
            {
                MunoExchangeRewardService.DestroyItems(rewards);
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            int remaining = count;
            foreach (Thing tape in tapes)
            {
                int take = System.Math.Min(remaining, tape.stackCount);
                tape.SplitOff(take).Destroy();
                remaining -= take;
                if (remaining == 0) break;
            }
            Messages.Message("已上交 " + count + " 份录像带，节目组的随机物资正在投放。", MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
