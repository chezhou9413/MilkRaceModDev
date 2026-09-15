using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MunoRaceLib.MunoWorld;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //持有任务独占的成员和物资候选奖励，保存领取状态并保证只交付一次。
    public class QuestPart_MunoRewards : QuestPart, IThingHolder
    {
        private ThingOwner<Thing> contents;
        private Pawn rewardPawn;
        private List<Thing> bonusItems = new List<Thing>();
        private bool prepared;
        private bool paid;

        //创建不会在等待选择期间推进人物生理状态的任务奖励容器。
        public QuestPart_MunoRewards()
        {
            contents = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        //任务部件本身作为持有链根节点。
        public IThingHolder ParentHolder => null;

        //返回唯一持有的未投放奖励。
        public ThingOwner GetDirectlyHeldThings()
        {
            return contents;
        }

        //向存档和对象遍历提供奖励人物的装备等子容器。
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, contents);
        }

        //生成一次物资候选，并复用已有缪诺成员生成器或现存受保护人物。
        public void Prepare(QuestPart_MunoMilitary mission, Pawn existingPawn)
        {
            if (prepared) return;
            rewardPawn = existingPawn;
            if (rewardPawn == null)
            {
                if (!MunoExchangeRewardService.TryGenerateMunoPawns(1, out List<Pawn> pawns, out string reason))
                    throw new InvalidOperationException(reason);
                rewardPawn = pawns.Single();
                if (Find.WorldPawns.Contains(rewardPawn)) Find.WorldPawns.RemovePawn(rewardPawn);
                if (!contents.TryAddOrTransfer(rewardPawn)) throw new InvalidOperationException("无法持有缪诺成员奖励。");
            }
            GenerateItems(mission, mission.Config.rewardValue, false);
            if (mission.BonusRewardValue > 0f) GenerateItems(mission, mission.BonusRewardValue, true);
            prepared = true;
        }

        //按独立预算生成普通或附加物资，并保持各物品身份以供两种奖励分支准确选取。
        private void GenerateItems(QuestPart_MunoMilitary mission, float value, bool bonus)
        {
            RewardsGeneratorParams parms = new RewardsGeneratorParams
            {
                rewardValue = value,
                giverFaction = mission.giver,
                minGeneratedRewardValue = 250f,
                thingRewardRequired = true,
                thingRewardItemsOnly = true,
                allowGoodwill = false,
                allowRoyalFavor = false,
                allowDevelopmentPoints = false,
                allowXenogermReimplantation = false
            };
            List<Reward> rewards = RewardsGenerator.Generate(parms, out _);
            int count = 0;
            foreach (Reward_Items reward in rewards.OfType<Reward_Items>())
                foreach (Thing item in reward.ItemsListForReading)
                {
                    if (!contents.TryAddOrTransfer(item, canMergeWithExistingStacks: false))
                        throw new InvalidOperationException("无法持有任务物资奖励。");
                    if (bonus) bonusItems.Add(item);
                    count++;
                }
            if (count == 0) throw new InvalidOperationException("原版奖励生成器未生成物资候选。");
        }

        //展示成员身份、技能、健康与具体物资，关闭窗口时仍保留选择信件。
        public void SendChoiceLetter()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("任务目标已完成，请选择一项奖励。选择成员时保留其当前伤势与装备。");
            text.AppendLine("\n成员：" + rewardPawn.LabelShortCap + "，" + rewardPawn.ageTracker.AgeBiologicalYears + " 岁");
            text.AppendLine("健康：" + rewardPawn.health.summaryHealth.SummaryHealthPercent.ToStringPercent());
            foreach (SkillRecord skill in rewardPawn.skills.skills.Where(s => !s.TotallyDisabled))
                text.AppendLine(skill.def.LabelCap + "：" + skill.Level);
            text.AppendLine("武器：" + (rewardPawn.equipment.Primary?.LabelCap ?? "无"));
            text.AppendLine("特性：" + string.Join("、", rewardPawn.story.traits.allTraits.Select(t => t.LabelCap.ToString())));
            text.AppendLine("服装：" + string.Join("、", rewardPawn.apparel.WornApparel.Select(a => a.LabelCap.ToString())));
            text.AppendLine("\n物资候选：");
            foreach (Thing item in contents.Where(t => !(t is Pawn) && !bonusItems.Contains(t))) text.AppendLine("• " + item.LabelCap);
            if (bonusItems.Count > 0)
            {
                text.AppendLine("\n情报附加物资（无论选择成员还是物资，都会获得）：");
                foreach (Thing item in bonusItems) text.AppendLine("• " + item.LabelCap);
            }
            QuestPart_MunoMilitary mission = quest.PartsListForReading.OfType<QuestPart_MunoMilitary>().Single();
            if (mission.Config.goodwillReward > 0)
                text.AppendLine("\n任务最终完成时增加缪诺好感：" + mission.Config.goodwillReward + "。");
            if (quest.PartsListForReading.OfType<QuestPart_MunoProtect>().Any())
                text.AppendLine("\n选择物资后，请先将受保护队员送上接收穿梭机，起飞后物资才会送达。");
            ChoiceLetter_MunoQuestReward letter = (ChoiceLetter_MunoQuestReward)LetterMaker.MakeLetter(
                "缪诺任务奖励", text.ToString(), DefDatabase<LetterDef>.GetNamed("Muno_QuestRewardChoice"),
                LookTargets.Invalid, null, quest);
            letter.title = "缪诺任务奖励";
            letter.rewards = this;
            Find.LetterStack.ReceiveLetter(letter);
        }

        //将玩家选择交给任务控制器，保护任务的物资必须等待队员撤离。
        public void Choose(bool takePawn)
        {
            if (paid || !prepared) return;
            quest.PartsListForReading.OfType<QuestPart_MunoMilitary>().Single().ChooseReward(takePawn);
        }

        //通过既有地图投放服务交付奖励，现存受保护队员无需重新投放。
        public void Deliver(QuestPart_MunoMilitary mission, bool takePawn)
        {
            if (paid) throw new InvalidOperationException("该任务的奖励已经领取。");
            if (!prepared) throw new InvalidOperationException("该任务尚未准备奖励。");
            List<Pawn> pawns = new List<Pawn>();
            if (takePawn && mission is QuestPart_MunoProtect)
            {
                if (rewardPawn != mission.subject || rewardPawn.Dead) throw new InvalidOperationException("受保护成员已失效。");
                rewardPawn.guest.SetGuestStatus(null);
                rewardPawn.SetFaction(Faction.OfPlayer);
            }
            else if (takePawn) pawns.Add(rewardPawn);
            List<Thing> items = takePawn ? new List<Thing>(bonusItems) : contents.Where(t => !(t is Pawn)).ToList();
            if (pawns.Count > 0 || items.Count > 0)
            {
                //交付前释放任务持有权，由原版运输舱接管，避免同一对象被双重保存。
                foreach (Thing thing in pawns.Cast<Thing>().Concat(items)) contents.Remove(thing);
                if (!MunoExchangeRewardService.TryDeliverRewardsToMap(mission.map, pawns, items, out string reason))
                    throw new InvalidOperationException(reason);
            }
            bonusItems.Clear();
            paid = true;
        }

        //仅清理仍由奖励容器持有的未选择对象。
        public override void Cleanup()
        {
            base.Cleanup();
            contents.ClearAndDestroyContentsOrPassToWorld();
            bonusItems.Clear();
        }

        //保存容器和人物引用，受保护人物始终只引用其原有地图持有链。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref contents, "contents", this);
            Scribe_References.Look(ref rewardPawn, "rewardPawn");
            Scribe_Collections.Look(ref bonusItems, "bonusItems", LookMode.Reference);
            Scribe_Values.Look(ref prepared, "prepared");
            Scribe_Values.Look(ref paid, "paid");
        }
    }
}
