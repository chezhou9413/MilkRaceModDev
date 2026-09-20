using System.Collections.Generic;
using System.Linq;
using MunoRaceLib.MunoScenarios;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoEndings
{
    //提供融入聚落的主动申请入口，复用穿梭机完成各地图成员的实际交付。
    public static class MunoEndingService
    {
        //列出本地图将交付的成员和全局剩余人数，确认后启动接收流程。
        public static void Open(Pawn negotiator)
        {
            MunoEndingComponent ending = Current.Game.GetComponent<MunoEndingComponent>();
            if (ending.Completed)
            {
                Reject("本局已经达成融入缪诺结局。");
                return;
            }
            if (Current.Game.GetComponent<MunoStoryComponent>().startTick < 0)
            {
                Reject("融入结局适用于缪诺的三种剧情开局。");
                return;
            }
            if (ending.Ready)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("已完成全部非缪诺成员的交付。确认融入聚落并达成结局？结局后可以继续游戏。", ending.Complete));
                return;
            }
            if (negotiator?.Map == null || !negotiator.IsColonistPlayerControlled || negotiator.Downed)
            {
                Reject("需要一名地图上的可行动殖民者联络聚落。");
                return;
            }
            if (!MunoStoryUtility.HasMunoMember())
            {
                Reject("至少需要一名仍属于玩家的缪诺成员才能申请融入聚落。");
                return;
            }
            List<Pawn> members = MunoStoryUtility.EndingMembers();
            List<Pawn> local = members.Where(p => p.Spawned && p.Map == negotiator.Map).ToList();
            if (local.Count == 0)
            {
                Reject(members.Count == 0 ? "需要通过实际上交正式成员达成结局，死亡或驱逐不算交付。"
                    : "其余非缪诺成员位于其他地图、远行队或容器中，请让他们在接收地图上集合。");
                return;
            }
            string names = string.Join("、", local.Select(p => p.LabelShortCap.ToString()));
            string text = "将向缪诺聚落交付：" + names + "。\n\n统计范围为所有地图、远行队及运输中的非缪诺正式殖民者和奴隶，包含儿童，排除囚犯和临时访客。"
                + "\n本次交付 " + local.Count + " 人，其余位置还有 " + (members.Count - local.Count) + " 人。"
                + "\n本次是融入申请，不发人口交换奖励；最后一批成功离场后达成结局。结局后缪诺留在当前地图，可以继续游戏。";
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () => Start(negotiator, local)));
        }

        //以无物资奖励的专用接收模式启动申请，并重新验证目标资格。
        private static void Start(Pawn negotiator, List<Pawn> pawns)
        {
            var targets = pawns.Select(p => new MunoExchangeTargetRecord
            {
                pawn = p,
                rewardType = MunoExchangeRewardType.RandomItems
            }).ToList();
            if (!MunoShuttleExchangeService.TryStartExchange(negotiator, targets, new List<Thing>(), out string reason, true))
                Reject(reason);
        }

        //向玩家显示无法申请的具体原因。
        private static void Reject(string text) => Messages.Message(text, MessageTypeDefOf.RejectInput, false);
    }
}
