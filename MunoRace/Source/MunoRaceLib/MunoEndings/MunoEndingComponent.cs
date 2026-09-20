using System.Collections.Generic;
using System.Linq;
using MunoRaceLib.MunoPursuit;
using MunoRaceLib.MunoScenarios;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoEndings
{
    //记录正式成员的实际交付时间，只在最后一次交付清空非缪诺成员时授予结局资格。
    public class MunoEndingComponent : GameComponent
    {
        private List<string> deliveredIds = new List<string>();
        private int readyTick = -1;
        private bool completed;

        //创建存档中的结局记录。
        public MunoEndingComponent(Game game) { }

        //返回结局是否已经达成。
        public bool Completed => completed;

        //返回仍然有效的交付完成资格。
        public bool Ready => !completed && readyTick >= 0 && deliveredIds.Count > 0
            && MunoStoryUtility.EndingMembers().Count == 0 && MunoStoryUtility.HasMunoMember();

        //在实际交付后记录仍存活的正式成员，并按交付完成时刻确定资格。
        public void RecordDelivery(IEnumerable<Pawn> pawns, List<string> memberIds, bool requestEnding)
        {
            if (completed || Current.Game.GetComponent<MunoStoryComponent>().startTick < 0) return;
            int accepted = 0;
            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || pawn.Dead || pawn.Destroyed || !memberIds.Contains(pawn.ThingID)) continue;
                if (deliveredIds.Contains(pawn.ThingID)) continue;
                deliveredIds.Add(pawn.ThingID);
                accepted++;
            }
            if (accepted == 0) return;
            readyTick = MunoStoryUtility.EndingMembers().Count == 0 ? Find.TickManager.TicksGame : -1;
            if (requestEnding && Ready) Complete();
        }

        //在出现新的非缪诺正式成员后撤销上一次交付产生的资格。
        public void RefreshEligibility()
        {
            if (!completed && readyTick >= 0 && MunoStoryUtility.EndingMembers().Count > 0) readyTick = -1;
        }

        //显示结局文字、停止追击，并允许玩家继续当前殖民地或返回主菜单。
        public void Complete()
        {
            if (!Ready) return;
            completed = true;
            Current.Game.GetComponent<MunoPursuitComponent>().Stop("聚落接受了你们的融入申请，追击令已撤销。");
            int elapsed = readyTick - Current.Game.GetComponent<MunoStoryComponent>().startTick;
            bool early = elapsed < MunoStoryUtility.DaysToTicks(MunoStoryConfigDef.Current.earlyEndingDays);
            string days = MunoStoryConfigDef.Current.earlyEndingDays.ToString("0.##");
            string title = early ? "开局跑路结局" : "跑路结局";
            string text = title + "\n\n" + (early
                ? "落地还不到 " + days + " 天，你们就把最后一名非缪诺成员交给了聚落。短暂的开拓结束了，聚落为留下的缪诺安排了新的身份。"
                : "经历这段边缘世界生活后，你们将最后一名非缪诺成员交给了聚落。独自生存的计划告一段落，留下的缪诺选择融入聚落。")
                + "\n\n这一段故事已经结束。你可以继续经营当前殖民地，也可以返回主菜单。";
            DiaNode node = new DiaNode(text);
            node.options.Add(new DiaOption("继续游戏") { resolveTree = true });
            node.options.Add(new DiaOption("返回主菜单") { action = () => GenScene.GoToMainMenu(), resolveTree = true });
            Find.WindowStack.Add(new Dialog_NodeTree(node, delayInteractivity: true));
            Find.Archive.Add(new ArchivedDialog(text));
        }

        //持久化已交付成员标识、最后有效交付时刻和结局完成状态。
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref deliveredIds, "deliveredIds", LookMode.Value);
            Scribe_Values.Look(ref readyTick, "readyTick", -1);
            Scribe_Values.Look(ref completed, "completed");
        }
    }
}
