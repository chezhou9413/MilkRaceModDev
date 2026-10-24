using System.Collections.Generic;
using System.Linq;
using MunoRaceLib.MunoScenarios;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoRecording
{
    //通过上传站消费实体录像带，并按独立奖励池投放随机物资。
    public static class MunoRecordingExchange
    {
        //说明当前操作者无法上传的实际原因。
        public static string UnavailableReason(Pawn pawn)
        {
            Faction faction = MunoStoryUtility.Faction;
            if (pawn?.Map == null || !pawn.IsColonistPlayerControlled || pawn.Downed
                || faction == null || faction.defeated || faction.deactivated || faction.HostileTo(Faction.OfPlayer))
            {
                return "需要可行动的殖民者，以及仍存在且不敌对的缪诺聚落。";
            }
            return null;
        }

        //统计当前地图可以上传的录像带数量。
        public static int AvailableTapeCount(Map map) => Tapes(map).Sum(t => t.stackCount);

        //收集地图中已显露且未禁用的实体录像带。
        private static List<Thing> Tapes(Map map)
        {
            return map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Muno_RecordingTape"))
                .Where(t => t.Spawned && !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map)).ToList();
        }

        //重新核对库存，成功投放奖励后精确扣除本次数量，避免重复领取。
        public static void Upload(Pawn negotiator, Building_MunoUploadStation station, int count)
        {
            string unavailable = UnavailableReason(negotiator);
            if (unavailable != null)
            {
                Messages.Message(unavailable, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!station.Spawned || station.Map != negotiator.Map || station.Faction != Faction.OfPlayer
                || negotiator.Position != station.InteractionCell || station.IsForbidden(negotiator) || count <= 0)
            {
                Messages.Message("需要在可用的录像上传站旁完成上传。", MessageTypeDefOf.RejectInput, false);
                return;
            }
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
