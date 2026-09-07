using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //提供可暂缓处理且随存档保留的军事任务奖励二选一信件。
    public class ChoiceLetter_MunoQuestReward : ChoiceLetter
    {
        public QuestPart_MunoRewards rewards;

        //禁止右键直接丢弃仍未完成的奖励选择。
        public override bool CanDismissWithRightClick => false;

        //任务结束后只保留档案，避免失效信件继续出现在通知栈。
        public override bool CanShowInLetterStack => base.CanShowInLetterStack && quest != null && !quest.Historical;

        //构造两种奖励选择、定位任务及稍后处理按钮。
        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (ArchivedOnly || quest == null || quest.Historical) { yield return Option_Close; yield break; }
                yield return MakeRewardOption("选择缪诺成员", true);
                yield return MakeRewardOption("选择物资奖励", false);
                yield return Option_Postpone;
            }
        }

        //校验任务当前状态后执行唯一奖励选择，并移除已处理的通知。
        private DiaOption MakeRewardOption(string label, bool takePawn)
        {
            DiaOption option = new DiaOption(label) { resolveTree = true };
            QuestPart_MunoMilitary mission = quest.PartsListForReading.OfType<QuestPart_MunoMilitary>().Single();
            if (!mission.CanChooseReward(out string reason)) option.Disable(reason);
            option.action = () =>
            {
                if (!mission.CanChooseReward(out string currentReason)) { MunoQuestUtility.Reject(currentReason); return; }
                rewards.Choose(takePawn);
                Find.LetterStack.RemoveLetter(this);
            };
            return option;
        }

        //保存奖励部件引用，读档后继续处理原来的候选结果。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref rewards, "rewards");
        }
    }
}
