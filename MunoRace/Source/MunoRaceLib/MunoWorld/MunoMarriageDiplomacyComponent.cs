using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责跨存档保存缪诺和亲请求、候选公主和后续外交事件状态。
    /// </summary>
    public class MunoMarriageDiplomacyComponent : GameComponent
    {
        private List<MunoMarriageCandidateRecord> candidates = new List<MunoMarriageCandidateRecord>();
        private List<MunoMarriageCandidateRecord> acceptedPrincesses = new List<MunoMarriageCandidateRecord>();
        private List<Thing> scheduledDowry = new List<Thing>();
        private int consumedYear = -999999;
        private int requestYear = -999999;
        private int requestExpireTick = -1;
        private bool requestPending;
        private int acceptedRequestCount;
        private bool naturalGoodwillRewardActive;
        private bool naturalGoodwillPenaltyActive;
        private int scheduledDeliveryTick = -1;
        private Pawn scheduledPrincess;
        private Map scheduledDeliveryMap;
        private Thing deliveryShuttle;
        private Pawn deliveryPrincess;
        private Map deliveryMap;

        /// <summary>
        /// 初始化游戏组件。
        /// </summary>
        public MunoMarriageDiplomacyComponent(Game game)
        {
        }

        /// <summary>
        /// 返回当前保存的和亲候选人列表。
        /// </summary>
        public List<MunoMarriageCandidateRecord> Candidates => candidates;

        /// <summary>
        /// 返回已经接受并需要后续追踪的和亲公主列表。
        /// </summary>
        public List<MunoMarriageCandidateRecord> AcceptedPrincesses => acceptedPrincesses;

        /// <summary>
        /// 返回当前是否有等待玩家回复的主动和亲请求。
        /// </summary>
        public bool RequestPending => requestPending;

        /// <summary>
        /// 返回主动和亲请求的过期绝对 Tick。
        /// </summary>
        public int RequestExpireTick => requestExpireTick;

        /// <summary>
        /// 返回已经接受主动和亲请求的次数。
        /// </summary>
        public int AcceptedRequestCount => acceptedRequestCount;

        /// <summary>
        /// 返回是否已经因接受和亲激活自然好感奖励。
        /// </summary>
        public bool NaturalGoodwillRewardActive => naturalGoodwillRewardActive;

        /// <summary>
        /// 返回是否已经因虐待和亲公主激活自然好感惩罚。
        /// </summary>
        public bool NaturalGoodwillPenaltyActive => naturalGoodwillPenaltyActive;

        /// <summary>
        /// 保存或读取和亲外交系统的全部存档数据。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref candidates, "candidates", LookMode.Deep);
            Scribe_Collections.Look(ref acceptedPrincesses, "acceptedPrincesses", LookMode.Deep);
            Scribe_Collections.Look(ref scheduledDowry, "scheduledDowry", LookMode.Deep);
            Scribe_Values.Look(ref consumedYear, "consumedYear", -999999);
            Scribe_Values.Look(ref requestYear, "requestYear", -999999);
            Scribe_Values.Look(ref requestExpireTick, "requestExpireTick", -1);
            Scribe_Values.Look(ref requestPending, "requestPending", defaultValue: false);
            Scribe_Values.Look(ref acceptedRequestCount, "acceptedRequestCount", defaultValue: 0);
            Scribe_Values.Look(ref naturalGoodwillRewardActive, "naturalGoodwillRewardActive", defaultValue: false);
            Scribe_Values.Look(ref naturalGoodwillPenaltyActive, "naturalGoodwillPenaltyActive", defaultValue: false);
            Scribe_Values.Look(ref scheduledDeliveryTick, "scheduledDeliveryTick", -1);
            Scribe_References.Look(ref scheduledPrincess, "scheduledPrincess");
            Scribe_References.Look(ref scheduledDeliveryMap, "scheduledDeliveryMap");
            Scribe_References.Look(ref deliveryShuttle, "deliveryShuttle");
            Scribe_References.Look(ref deliveryPrincess, "deliveryPrincess");
            Scribe_References.Look(ref deliveryMap, "deliveryMap");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (candidates == null)
                {
                    candidates = new List<MunoMarriageCandidateRecord>();
                }

                if (acceptedPrincesses == null)
                {
                    acceptedPrincesses = new List<MunoMarriageCandidateRecord>();
                }

                if (scheduledDowry == null)
                {
                    scheduledDowry = new List<Thing>();
                }

                CleanupInvalidRecords();
                scheduledDowry.RemoveAll(thing => thing == null || thing.Destroyed);
            }
        }

        /// <summary>
        /// 每 Tick 检查主动请求触发、请求超时、延迟送达和穿梭机落地流程。
        /// </summary>
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 120 == 0)
            {
                MunoMarriageRequestUtility.TickRequestState(this);
                TryStartScheduledDelivery();
                TryFinalizeDelivery();
            }
        }

        /// <summary>
        /// 返回当前年份是否已有保存候选。
        /// </summary>
        public bool HasCandidates()
        {
            CleanupInvalidRecords();
            return candidates.Count > 0;
        }

        /// <summary>
        /// 返回当前年份是否还能生成或继续处理和亲请求。
        /// </summary>
        public bool CanUseRequestThisYear(int year)
        {
            if (HasActiveRequest())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 返回是否有未过期的主动和亲请求。
        /// </summary>
        public bool HasActiveRequest()
        {
            return requestPending && requestExpireTick > GenTicks.TicksAbs;
        }

        /// <summary>
        /// 返回本年是否还能触发新的主动和亲请求。
        /// </summary>
        public bool CanTriggerNewRequest(int year)
        {
            return consumedYear != year && !requestPending && acceptedRequestCount < 2 && scheduledPrincess == null && deliveryPrincess == null && deliveryShuttle == null && !HasCandidates();
        }

        /// <summary>
        /// 将本年标记为已经消耗和亲请求。
        /// </summary>
        public void MarkYearConsumed(int year)
        {
            consumedYear = year;
        }

        /// <summary>
        /// 替换当前候选人列表。
        /// </summary>
        public void SetCandidates(List<MunoMarriageCandidateRecord> newCandidates)
        {
            candidates = newCandidates ?? new List<MunoMarriageCandidateRecord>();
        }

        /// <summary>
        /// 开始等待玩家回复的主动和亲请求。
        /// </summary>
        public void StartPendingRequest(int year, int expireTick)
        {
            requestYear = year;
            requestExpireTick = expireTick;
            requestPending = true;
        }

        /// <summary>
        /// 将当前主动和亲请求标记为已拒绝或已超时。
        /// </summary>
        public void FinishPendingRequest()
        {
            requestPending = false;
            requestExpireTick = -1;
            MarkYearConsumed(requestYear == -999999 ? MunoMarriageDiplomacyService.CurrentYear() : requestYear);
        }

        /// <summary>
        /// 清空当前未接受的候选人列表。
        /// </summary>
        public void ClearCandidates()
        {
            candidates.Clear();
        }

        /// <summary>
        /// 把指定公主登记为已接受状态并保留后续事件追踪。
        /// </summary>
        public void RegisterAcceptedPrincess(Pawn princess)
        {
            MunoMarriageCandidateRecord record = FindRecord(princess);
            if (record == null)
            {
                record = new MunoMarriageCandidateRecord
                {
                    pawn = princess
                };
            }

            record.accepted = true;
            if (!acceptedPrincesses.Contains(record))
            {
                acceptedPrincesses.Add(record);
            }
        }

        /// <summary>
        /// 记录接受和亲后的延迟送达安排。
        /// </summary>
        public void ScheduleDelivery(Pawn princess, Map map, List<Thing> dowry, int deliveryTick)
        {
            scheduledPrincess = princess;
            scheduledDeliveryMap = map;
            scheduledDeliveryTick = deliveryTick;
            scheduledDowry = dowry ?? new List<Thing>();
            acceptedRequestCount++;
            requestPending = false;
            requestExpireTick = -1;
            MarkYearConsumed(requestYear == -999999 ? MunoMarriageDiplomacyService.CurrentYear() : requestYear);
        }

        /// <summary>
        /// 记录正在送达的穿梭机和公主。
        /// </summary>
        public void StartDelivery(Thing shuttle, Pawn princess, Map map)
        {
            deliveryShuttle = shuttle;
            deliveryPrincess = princess;
            deliveryMap = map;
        }

        /// <summary>
        /// 尝试查找指定 Pawn 对应的和亲记录。
        /// </summary>
        public MunoMarriageCandidateRecord FindRecord(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i]?.pawn == pawn)
                {
                    return candidates[i];
                }
            }

            for (int i = 0; i < acceptedPrincesses.Count; i++)
            {
                if (acceptedPrincesses[i]?.pawn == pawn)
                {
                    return acceptedPrincesses[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 激活一次全局自然好感奖励并刷新原版好感情境缓存。
        /// </summary>
        public void ActivateNaturalGoodwillReward()
        {
            if (naturalGoodwillRewardActive)
            {
                return;
            }

            naturalGoodwillRewardActive = true;
            Find.GoodwillSituationManager.RecalculateAll(canSendHostilityChangedLetter: true);
        }

        /// <summary>
        /// 激活一次全局自然好感惩罚并刷新原版好感情境缓存。
        /// </summary>
        public void ActivateNaturalGoodwillPenalty()
        {
            if (naturalGoodwillPenaltyActive)
            {
                return;
            }

            naturalGoodwillPenaltyActive = true;
            Find.GoodwillSituationManager.RecalculateAll(canSendHostilityChangedLetter: true);
        }

        /// <summary>
        /// 在延迟时间到达后创建穿梭机，并把公主与嫁妆装入穿梭机。
        /// </summary>
        public void TryStartScheduledDelivery()
        {
            if (scheduledPrincess == null || scheduledDeliveryTick < 0 || GenTicks.TicksAbs < scheduledDeliveryTick)
            {
                return;
            }

            Map map = ResolveDeliveryMap();
            if (map == null)
            {
                SendDeliveryFailedLetter("当前没有可用的玩家殖民地地图，缪诺和亲穿梭机无法入场。");
                ClearScheduledDelivery();
                return;
            }

            IntVec3 near = map.mapPawns.FreeColonistsSpawned.Count > 0 ? map.mapPawns.FreeColonistsSpawned.RandomElement().Position : map.Center;
            if (!MunoMarriageShuttleDeliveryService.TrySpawnPrincessShuttle(map, near, scheduledPrincess, scheduledDowry, out Thing shuttle, out string failReason))
            {
                SendDeliveryFailedLetter(failReason ?? "缪诺和亲穿梭机未能找到安全降落区域。");
                ClearScheduledDelivery();
                return;
            }

            StartDelivery(shuttle, scheduledPrincess, map);
            scheduledPrincess = null;
            scheduledDeliveryMap = null;
            scheduledDeliveryTick = -1;
            scheduledDowry = new List<Thing>();
        }

        /// <summary>
        /// 尝试完成穿梭机送达后的卸载、招募和清理。
        /// </summary>
        private void TryFinalizeDelivery()
        {
            if (deliveryShuttle == null || deliveryPrincess == null || deliveryMap == null)
            {
                return;
            }

            if (!deliveryShuttle.Spawned || deliveryShuttle.Map != deliveryMap)
            {
                return;
            }

            CompTransporter transporter = deliveryShuttle.TryGetComp<CompTransporter>();
            if (transporter == null)
            {
                return;
            }

            IntVec3 dropCell = deliveryShuttle.Position;
            if (transporter.innerContainer.Contains(deliveryPrincess))
            {
                Pawn droppedPrincess = null;
                bool droppedAll = transporter.innerContainer.TryDropAll(dropCell, deliveryMap, ThingPlaceMode.Near, (thing, count) =>
                {
                    if (thing == deliveryPrincess)
                    {
                        droppedPrincess = thing as Pawn;
                    }
                });

                if (!droppedAll || droppedPrincess == null)
                {
                    return;
                }

                FinishDelivery(droppedPrincess);
                return;
            }

            if (deliveryPrincess.Spawned && deliveryPrincess.Map == deliveryMap)
            {
                FinishDelivery(deliveryPrincess);
            }
        }

        /// <summary>
        /// 完成和亲公主送达后的招募、记录标记和穿梭机离场。
        /// </summary>
        private void FinishDelivery(Pawn droppedPrincess)
        {
            MunoMarriageDiplomacyService.NormalizePrincessAfterArrival(droppedPrincess);
            MunoMarriageCandidateRecord record = FindRecord(droppedPrincess);
            if (record != null)
            {
                record.delivered = true;
            }

            CompShuttle shuttleComp = deliveryShuttle.TryGetComp<CompShuttle>();
            shuttleComp?.shipParent?.ForceJob(ShipJobDefOf.FlyAway);
            deliveryShuttle = null;
            deliveryPrincess = null;
            deliveryMap = null;
        }

        /// <summary>
        /// 返回当前可用于接收送达穿梭机的玩家地图。
        /// </summary>
        private Map ResolveDeliveryMap()
        {
            if (scheduledDeliveryMap != null && Find.Maps.Contains(scheduledDeliveryMap) && scheduledDeliveryMap.IsPlayerHome)
            {
                return scheduledDeliveryMap;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                if (Find.Maps[i].IsPlayerHome)
                {
                    return Find.Maps[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 清理尚未开始入场的延迟送达数据。
        /// </summary>
        private void ClearScheduledDelivery()
        {
            scheduledPrincess = null;
            scheduledDeliveryMap = null;
            scheduledDeliveryTick = -1;
            scheduledDowry = new List<Thing>();
        }

        /// <summary>
        /// 发送和亲送达失败提示信。
        /// </summary>
        private static void SendDeliveryFailedLetter(string reason)
        {
            Find.LetterStack.ReceiveLetter(
                "缪诺和亲送达失败",
                "缪诺方面发来紧急讯息：" + reason + "\n\n本年度和亲安排已经关闭，她们不会在本年再次尝试送达。",
                LetterDefOf.NegativeEvent);
        }

        /// <summary>
        /// 移除已经失效的空记录，避免读档后残留空引用。
        /// </summary>
        private void CleanupInvalidRecords()
        {
            candidates.RemoveAll(record => record == null || record.pawn == null || record.pawn.Destroyed);
            acceptedPrincesses.RemoveAll(record => record == null || record.pawn == null || record.pawn.Destroyed);
        }
    }
}
