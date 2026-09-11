using System;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //为两类军事任务组装独立进度、奖励容器和临时客人归属。
    public abstract class QuestNode_MunoMilitary : QuestNode
    {
        //返回当前支线使用的任务定义名称。
        protected abstract string ScriptName { get; }

        //创建当前支线的阶段控制器。
        protected abstract QuestPart_MunoMilitary CreateMission();

        //检查自然邀请是否具备派系、地图、头衔和场地条件。
        protected override bool TestRunInt(Slate slate)
        {
            return MunoQuestUtility.CanOffer(ScriptName, slate, out _);
        }

        //仅建立可存档的任务描述与状态，地图事件由接受信号启动。
        protected override void RunInt()
        {
            if (!TestRunInt(QuestGen.slate)) throw new InvalidOperationException("缪诺军事任务生成前提不满足。");
            Quest quest = QuestGen.quest;
            QuestPart_MunoMilitary mission = CreateMission();
            mission.map = MunoQuestUtility.HomeMap(QuestGen.slate);
            mission.giver = MunoQuestUtility.Giver();
            mission.enemy = Find.FactionManager.AllFactions.Where(MunoQuestUtility.EligibleEnemy).RandomElement();
            mission.inSignalEnable = quest.InitiateSignal;
            quest.AddPart(mission);
            quest.AddPart(new QuestPart_MunoRewards());
            if (mission is QuestPart_MunoProtect)
            {
                quest.AddPart(new QuestPart_ExtraFaction
                {
                    extraFaction = new ExtraFaction(mission.giver, ExtraFactionType.HomeFaction),
                    inSignalEnable = quest.InitiateSignal,
                    areHelpers = false
                });
            }
            QuestGen.slate.Set("map", mission.map);
            quest.points = QuestGen.slate.Get<float>("points");
            SetDescriptionParameters(mission);
        }

        //将平衡参数写入原版任务文本变量，避免调整配置后描述仍显示旧数值。
        private static void SetDescriptionParameters(QuestPart_MunoMilitary mission)
        {
            MunoQuestConfig config = mission.Config;
            QuestGen.slate.Set("captureDays", config.captureDays.ToString("0.#"));
            QuestGen.slate.Set("captureCount", config.captureCount);
            QuestGen.slate.Set("intelligenceChance", config.intelligenceChance.ToStringPercent());
            QuestGen.slate.Set("intelligenceRewardValue", config.intelligenceRewardValue.ToString("0"));
            QuestGen.slate.Set("goodwillReward", config.goodwillReward);
            QuestGen.slate.Set("arrivalDays", config.arrivalDays.min.ToString("0.#") + "–" + config.arrivalDays.max.ToString("0.#"));
            QuestGen.slate.Set("shuttleDays", config.shuttleDays.ToString("0.#"));
            QuestGen.slate.Set("rewardValue", config.rewardValue.ToString("0"));
            QuestGen.slate.Set("skillRange", config.combatSkill.min + "–" + config.combatSkill.max);
            QuestGen.slate.Set("requiredTitle", DefDatabase<RoyalTitleDef>.GetNamed(config.requiredTitle).label);
            QuestGen.slate.Set("offerDays", mission.quest.root.expireDaysRange.max.ToString("0.#"));
        }
    }
}
