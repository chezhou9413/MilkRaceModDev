using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //管理军事支线的接受、计时、接收穿梭机和唯一奖励结算。
    public abstract class QuestPart_MunoMilitary : QuestPartActivable
    {
        public Map map;
        public Faction giver;
        public Faction enemy;
        public Pawn subject;
        protected int dueTick;
        protected bool rewardReady;
        protected bool settled;
        protected string progress = "等待接受委托。";
        protected MunoQuestShuttle pickup;

        //读取当前任务的平衡配置。
        public MunoQuestConfig Config => quest.root.GetModExtension<MunoQuestConfig>();

        //返回任务独占的待领取奖励容器。
        public QuestPart_MunoRewards Rewards => quest.PartsListForReading.OfType<QuestPart_MunoRewards>().Single();

        //显示任务当前阶段及指定人物。
        public override string DescriptionPart => progress + (subject == null ? "" : "\n指定人物：" + subject.LabelShortCap);

        //在被选中人物的检查栏中标明其所属任务。
        public override string ExtraInspectString(ISelectable target)
        {
            return !settled && target == subject ? "缪诺任务指定人物：" + quest.name : null;
        }

        //显示有明确期限的阶段剩余时间。
        public override string ExpiryInfoPart => dueTick > 0 && !settled
            ? "当前阶段剩余：" + Math.Max(0, dueTick - GenTicks.TicksGame).ToStringTicksToPeriod() : null;

        //在任务面板提供人物及任务基地跳转目标。
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                if (subject != null && !subject.Destroyed) yield return subject;
                if (map != null) yield return map.Parent;
            }
        }

        //登记任务涉及的友方和追捕派系。
        public override IEnumerable<Faction> InvolvedFactions
        {
            get { yield return giver; yield return enemy; }
        }

        //保留任务人物的世界引用，防止任务进行时被原版回收。
        public override bool QuestPartReserves(Pawn pawn)
        {
            return pawn == subject && !settled;
        }

        //仅在玩家接受后创建哨站或投放伤员。
        protected override void Enable(SignalArgs args)
        {
            base.Enable(args);
            try
            {
                if (!MunoQuestUtility.ValidHome(map) || giver.defeated || giver.HostileTo(Faction.OfPlayer))
                {
                    Fail("接受时基地或缪诺派系已不可用。");
                    return;
                }
                StartMission();
            }
            catch (Exception error) { FailException(error); }
        }

        //由具体任务创建接受后的初始事件。
        protected abstract void StartMission();

        //由具体任务检查人物状态并推进主要目标。
        protected abstract void TickMission();

        //每秒推进一次任务，生成失败时终止该任务并输出完整异常。
        public override void QuestPartTick()
        {
            if (settled || GenTicks.TicksGame % 60 != 0) return;
            try
            {
                if (!MunoQuestUtility.ValidHome(map)) { Fail("任务基地已丢失。"); return; }
                if (giver == null || giver.defeated || giver.HostileTo(Faction.OfPlayer)) { Fail("缪诺派系已无法继续委托。"); return; }
                //离场信号在原版搬运容器前发出，延后一帧结算以避开容器转移过程。
                if (pickup != null && pickup.Departed)
                {
                    OnPickupCompleted();
                    return;
                }
                if ((!rewardReady || this is QuestPart_MunoProtect) && (subject == null || subject.Dead || subject.Destroyed))
                { Fail("指定人物已死亡或失效。"); return; }
                TickMission();
                if (!settled && pickup != null && !pickup.Poll(subject, map, out string reason)) Fail(reason);
            }
            catch (Exception error) { FailException(error); }
        }

        //接收原版穿梭机装载起飞信号，仅记录状态不在回调中销毁对象。
        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (pickup != null) pickup.ReceiveSignal(signal, subject);
        }

        //在指定人物完成交接后转入领奖或物资发放。
        protected virtual void OnPickupCompleted()
        {
            pickup = null;
            OfferRewards();
        }

        //安排只接收指定人物的独立穿梭机。
        protected void StartPickup()
        {
            pickup = new MunoQuestShuttle();
            pickup.Start(quest, giver, map, subject, Config.shuttleDays);
            dueTick = pickup.ExpiryTick;
            progress = "接收穿梭机已出发，请在期限内装载指定人物。";
        }

        //准备固定奖励并发送可暂缓处理的选择信件。
        protected void OfferRewards()
        {
            if (rewardReady) return;
            Rewards.Prepare(this, this is QuestPart_MunoProtect ? subject : null);
            rewardReady = true;
            dueTick = 0;
            progress = "目标已完成，等待选择缪诺成员或物资奖励。";
            Rewards.SendChoiceLetter();
        }

        //检验领奖瞬间的任务与人物状态，避免暂停期间失效后继续领奖。
        public virtual bool CanChooseReward(out string reason)
        {
            reason = !settled && rewardReady && quest.State == QuestState.Ongoing && MunoQuestUtility.ValidHome(map)
                ? null : "当前任务不能领取奖励。";
            return reason == null;
        }

        //提交一次成员或物资奖励选择。
        public virtual void ChooseReward(bool takePawn)
        {
            if (!CanChooseReward(out string reason)) { MunoQuestUtility.Reject(reason); return; }
            try { Rewards.Deliver(this, takePawn); Finish(); }
            catch (Exception error) { FailException(error); }
        }

        //标记成功并让原版任务系统执行清理与成功通知。
        protected void Finish()
        {
            settled = true;
            progress = "委托完成，奖励已交付。";
            quest.End(QuestEndOutcome.Success);
        }

        //记录明确的失败原因并交由原版任务系统停止推进。
        public void Fail(string reason)
        {
            if (settled) return;
            settled = true;
            progress = "委托失败：" + reason;
            quest.description += "\n\n" + progress;
            quest.End(QuestEndOutcome.Fail);
        }

        //输出完整故障上下文并终止任务，避免每秒重复抛出同一异常。
        protected void FailException(Exception error)
        {
            Log.Error("[MunoRace] 军事任务 " + quest.id + " 执行失败：\n" + error);
            Fail(error.Message);
        }

        //解除未完成的穿梭机装载，不删除玩家基地内的人物。
        public override void Cleanup()
        {
            base.Cleanup();
            settled = true;
            pickup?.Cancel(map);
        }

        //保存任务人物引用、阶段时限与穿梭机状态。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref map, "map");
            Scribe_References.Look(ref giver, "giver");
            Scribe_References.Look(ref enemy, "enemy");
            Scribe_References.Look(ref subject, "subject");
            Scribe_Values.Look(ref dueTick, "dueTick");
            Scribe_Values.Look(ref rewardReady, "rewardReady");
            Scribe_Values.Look(ref settled, "settled");
            Scribe_Values.Look(ref progress, "progress");
            Scribe_Deep.Look(ref pickup, "pickup");
        }
    }
}
