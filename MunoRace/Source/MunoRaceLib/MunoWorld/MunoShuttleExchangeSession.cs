using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //保存批量缪诺穿梭机交换会话，并负责装载检测、失败清理和奖励结算。
    public class MunoShuttleExchangeSession : GameComponent
    {
        private Pawn negotiator;
        private List<MunoExchangeTargetRecord> targets = new List<MunoExchangeTargetRecord>();
        private List<Thing> pendingItemRewards = new List<Thing>();
        private Thing shuttle;
        private Map map;
        private int loadedTargetCount;
        private bool rewardGranted;
        private bool finished = true;
        private bool launchTriggered;
        private string failReason;

        //创建当前存档使用的缪诺穿梭机交换组件。
        public MunoShuttleExchangeSession(Game game)
        {
        }

        //返回当前是否存在尚未结束的穿梭机交换会话。
        public bool HasActiveSession => !finished;

        //返回本次交换绑定的目标记录。
        public List<MunoExchangeTargetRecord> TargetsForReading => targets;

        //返回本次交换的目标总数。
        public int TargetCount => targets?.Count ?? 0;

        //返回已经进入穿梭机的目标数量。
        public int LoadedTargetCount => loadedTargetCount;

        //返回是否已经装载全部目标。
        public bool AllTargetsLoaded => TargetCount > 0 && loadedTargetCount >= TargetCount;

        //返回当前会话使用的穿梭机。
        public Thing Shuttle => shuttle;

        //返回当前会话所在地图。
        public Map Map => map;

        //返回最近一次失败原因。
        public string FailReason => failReason;

        //定期检查批量装载状态，并在穿梭机离场后完成奖励结算。
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (finished || Find.TickManager.TicksGame % 30 != 0)
            {
                return;
            }

            if (shuttle == null)
            {
                if (launchTriggered && AllTargetsLoaded)
                {
                    GrantReward();
                }
                else
                {
                    MarkFailed("缪诺接收穿梭机引用已失效，本次流程已中止。", true);
                }
                return;
            }

            if (launchTriggered)
            {
                if (AllTargetsLoaded && (shuttle.Destroyed || shuttle.MapHeld == null))
                {
                    GrantReward();
                }
                return;
            }

            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            if (transporter == null)
            {
                MarkFailed("缪诺接收穿梭机缺少运输组件，本次流程已中止。", true);
                return;
            }

            loadedTargetCount = CountLoadedTargets(transporter);
            if (HasInvalidTarget(transporter))
            {
                AbortAndFail("至少一名接收目标已死亡或失去有效性，本次批量流程已中止。", transporter);
                return;
            }

            if (shuttle.Destroyed)
            {
                MarkFailed("缪诺接收穿梭机已损毁，本次流程已中止。", true);
                return;
            }

            if (shuttle.MapHeld == null)
            {
                MarkFailed("穿梭机在完成装载前离开了地图，本次流程已中止。", true);
                return;
            }

            if (!AllTargetsLoaded)
            {
                return;
            }

            launchTriggered = true;
            if (!TryLaunchShuttleNow())
            {
                launchTriggered = false;
                AbortAndFail("缪诺接收穿梭机未能进入离场流程，本次接收已中止。", transporter);
            }
        }

        //启动新的批量交换会话并接管锁定的随机物资对象。
        public void StartSession(Pawn newNegotiator, List<MunoExchangeTargetRecord> newTargets, List<Thing> itemRewards, Thing newShuttle, Map newMap)
        {
            negotiator = newNegotiator;
            targets = newTargets ?? new List<MunoExchangeTargetRecord>();
            pendingItemRewards = itemRewards ?? new List<Thing>();
            shuttle = newShuttle;
            map = newMap;
            loadedTargetCount = 0;
            rewardGranted = false;
            finished = false;
            launchTriggered = false;
            failReason = null;
        }

        //持久化会话目标、锁定物资、穿梭机引用和结算状态。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref negotiator, "munoExchangeNegotiator");
            Scribe_Collections.Look(ref targets, "munoExchangeTargets", LookMode.Deep);
            Scribe_Collections.Look(ref pendingItemRewards, "munoExchangePendingItems", LookMode.Deep);
            Scribe_References.Look(ref shuttle, "munoExchangeShuttle");
            Scribe_References.Look(ref map, "munoExchangeMap");
            Scribe_Values.Look(ref loadedTargetCount, "munoExchangeLoadedTargetCount", 0);
            Scribe_Values.Look(ref rewardGranted, "munoExchangeRewardGranted", false);
            Scribe_Values.Look(ref finished, "munoExchangeFinished", true);
            Scribe_Values.Look(ref launchTriggered, "munoExchangeLaunchTriggered", false);
            Scribe_Values.Look(ref failReason, "munoExchangeFailReason");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                targets = targets ?? new List<MunoExchangeTargetRecord>();
                targets.RemoveAll(target => target?.pawn == null);
                pendingItemRewards = pendingItemRewards ?? new List<Thing>();
                pendingItemRewards.RemoveAll(item => item == null);
            }
        }

        //统计当前运输容器中已经装载的目标数量。
        private int CountLoadedTargets(CompTransporter transporter)
        {
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                Pawn pawn = targets[i]?.pawn;
                if (pawn != null && transporter.innerContainer.Contains(pawn))
                {
                    count++;
                }
            }

            return count;
        }

        //判断未装载目标是否已经死亡或彻底离开当前地图持有链。
        private bool HasInvalidTarget(CompTransporter transporter)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                Pawn pawn = targets[i]?.pawn;
                if (pawn == null || pawn.Dead || pawn.Destroyed)
                {
                    return true;
                }

                if (transporter.innerContainer.Contains(pawn))
                {
                    continue;
                }

                if (pawn.MapHeld != map)
                {
                    return true;
                }
            }

            return false;
        }

        //取消当前装载、卸下已进入穿梭机的目标并安排穿梭机离场。
        private void AbortAndFail(string reason, CompTransporter transporter)
        {
            if (transporter != null && map != null)
            {
                transporter.CancelLoad(map);
            }

            CompShuttle shuttleComp = shuttle?.TryGetComp<CompShuttle>();
            if (shuttleComp != null)
            {
                shuttleComp.requiredPawns.Clear();
                shuttleComp.shipParent?.ForceJob(ShipJobDefOf.FlyAway);
            }

            MarkFailed(reason, true);
        }

        //在全部目标完成装载后强制穿梭机离场。
        private bool TryLaunchShuttleNow()
        {
            CompShuttle shuttleComp = shuttle.TryGetComp<CompShuttle>();
            if (shuttleComp?.shipParent == null)
            {
                return false;
            }

            shuttleComp.shipParent.ForceJob(ShipJobDefOf.FlyAway);
            return true;
        }

        //按目标记录统计缪诺成员奖励数量。
        private int CountMunoRewards()
        {
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].rewardType == MunoExchangeRewardType.MunoPawn)
                {
                    count++;
                }
            }

            return count;
        }

        //生成并投放本次批量交换的全部奖励。
        private void GrantReward()
        {
            if (rewardGranted)
            {
                return;
            }

            if (map == null)
            {
                MarkFailed("交换地图已失效，本次流程已中止。", true);
                return;
            }

            int munoRewardCount = CountMunoRewards();
            if (!MunoExchangeRewardService.TryGenerateMunoPawns(munoRewardCount, out List<Pawn> rewardPawns, out string generateFailReason))
            {
                MarkFailed(generateFailReason ?? "未能生成缪诺奖励成员，本次流程已中止。", true);
                return;
            }

            if (!MunoExchangeRewardService.TryDeliverRewardsToMap(map, rewardPawns, pendingItemRewards, out string placeFailReason))
            {
                MunoExchangeRewardService.DestroyPawns(rewardPawns);
                MarkFailed(placeFailReason ?? "未能投放缪诺交换奖励，本次流程已中止。", true);
                return;
            }

            int itemCount = pendingItemRewards.Count;
            pendingItemRewards = new List<Thing>();
            rewardGranted = true;
            finished = true;
            Messages.Message("缪诺已成功接收 " + TargetCount + " 名目标，" + munoRewardCount + " 名缪诺成员与 " + itemCount + " 组物资已送达。", MessageTypeDefOf.PositiveEvent, false);
        }

        //将会话标记为失败，并按需要清理尚未交付的随机物资。
        private void MarkFailed(string reason, bool destroyPendingItems)
        {
            if (finished)
            {
                return;
            }

            if (destroyPendingItems)
            {
                MunoExchangeRewardService.DestroyItems(pendingItemRewards);
                pendingItemRewards.Clear();
            }

            failReason = reason;
            finished = true;
            if (!reason.NullOrEmpty())
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
            }
        }

        //清理会话引用，并按需要销毁尚未交付的物资。
        private void ClearSession(bool destroyPendingItems)
        {
            if (destroyPendingItems)
            {
                MunoExchangeRewardService.DestroyItems(pendingItemRewards);
            }

            negotiator = null;
            targets.Clear();
            pendingItemRewards.Clear();
            shuttle = null;
            map = null;
            loadedTargetCount = 0;
            rewardGranted = false;
            finished = true;
            launchTriggered = false;
            failReason = null;
        }
    }
}
