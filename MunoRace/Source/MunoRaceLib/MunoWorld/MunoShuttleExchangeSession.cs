using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责保存当前存档中的缪诺穿梭机交换会话，并驱动离场成功后的奖励结算。
    /// </summary>
    public class MunoShuttleExchangeSession : GameComponent
    {
        private Pawn negotiator;
        private Pawn selectedPawn;
        private Thing shuttle;
        private Map map;
        private bool rewardGranted;
        private bool finished;
        private bool launchTriggered;
        private bool targetLoaded;
        private string failReason;

        /// <summary>
        /// 为缪诺穿梭机交换会话创建一个新的游戏级状态组件。
        /// </summary>
        public MunoShuttleExchangeSession(Game game)
        {
        }

        /// <summary>
        /// 返回当前是否存在尚未结束的交换会话。
        /// </summary>
        public bool HasActiveSession => !finished && selectedPawn != null && shuttle != null;

        /// <summary>
        /// 返回当前会话绑定的唯一目标 Pawn。
        /// </summary>
        public Pawn SelectedPawn => selectedPawn;

        /// <summary>
        /// 返回当前会话使用的穿梭机对象。
        /// </summary>
        public Thing Shuttle => shuttle;

        /// <summary>
        /// 返回当前会话所在地图。
        /// </summary>
        public Map Map => map;

        /// <summary>
        /// 返回当前会话失败原因，供 UI 或消息提示复用。
        /// </summary>
        public string FailReason => failReason;

        /// <summary>
        /// 在游戏 Tick 中持续检查穿梭机状态，并在目标成功离场后发放奖励。
        /// </summary>
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (finished || shuttle == null)
            {
                return;
            }

            if (Find.TickManager.TicksGame % 30 != 0)
            {
                return;
            }

            if (selectedPawn == null || selectedPawn.Dead || selectedPawn.Destroyed)
            {
                MarkFailed("目标已死亡或失去有效性，本次缪诺接收流程已中止。");
                return;
            }

            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            bool pawnLoaded = transporter != null && transporter.innerContainer.Contains(selectedPawn);
            if (pawnLoaded)
            {
                targetLoaded = true;
            }

            if (shuttle.Destroyed)
            {
                if (rewardGranted)
                {
                    ClearSession();
                }
                else if (launchTriggered && targetLoaded)
                {
                    GrantReward();
                }
                else
                {
                    MarkFailed("缪诺接收穿梭机已损毁，本次流程已中止。");
                }
                return;
            }

            if (transporter == null)
            {
                if (launchTriggered && targetLoaded)
                {
                    GrantReward();
                }
                else
                {
                    MarkFailed("缪诺接收穿梭机缺少运输组件，本次流程已中止。");
                }
                return;
            }

            bool shuttleGoneFromMap = shuttle.MapHeld == null;
            if (pawnLoaded && !launchTriggered)
            {
                if (TryLaunchShuttleNow())
                {
                    launchTriggered = true;
                }
                else
                {
                    MarkFailed("缪诺接收穿梭机未能进入离场流程，本次接收已中止。");
                    return;
                }
            }

            if (!rewardGranted && pawnLoaded && shuttleGoneFromMap)
            {
                GrantReward();
                return;
            }

            if (!rewardGranted && !pawnLoaded && shuttleGoneFromMap)
            {
                MarkFailed("穿梭机已离场，但目标未被成功接收，本次流程已中止。");
            }
        }

        /// <summary>
        /// 启动新的缪诺穿梭机交换会话，并绑定谈判者、目标与穿梭机。
        /// </summary>
        public void StartSession(Pawn newNegotiator, Pawn newSelectedPawn, Thing newShuttle, Map newMap)
        {
            negotiator = newNegotiator;
            selectedPawn = newSelectedPawn;
            shuttle = newShuttle;
            map = newMap;
            rewardGranted = false;
            finished = false;
            launchTriggered = false;
            targetLoaded = false;
            failReason = null;
        }

        /// <summary>
        /// 将当前流程标记为失败，并向玩家发送一次明确消息。
        /// </summary>
        public void MarkFailed(string reason)
        {
            if (finished)
            {
                return;
            }

            failReason = reason;
            finished = true;
            if (!reason.NullOrEmpty())
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
            }
        }

        /// <summary>
        /// 清理本次交换会话，供流程完成或失败后复位使用。
        /// </summary>
        public void ClearSession()
        {
            negotiator = null;
            selectedPawn = null;
            shuttle = null;
            map = null;
            rewardGranted = false;
            finished = true;
            launchTriggered = false;
            targetLoaded = false;
            failReason = null;
        }

        /// <summary>
        /// 持久化当前交换会话的关键引用，保证读档后仍能继续检测状态。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref negotiator, "munoExchangeNegotiator");
            Scribe_References.Look(ref selectedPawn, "munoExchangeSelectedPawn");
            Scribe_References.Look(ref shuttle, "munoExchangeShuttle");
            Scribe_References.Look(ref map, "munoExchangeMap");
            Scribe_Values.Look(ref rewardGranted, "munoExchangeRewardGranted", false);
            Scribe_Values.Look(ref finished, "munoExchangeFinished", true);
            Scribe_Values.Look(ref launchTriggered, "munoExchangeLaunchTriggered", false);
            Scribe_Values.Look(ref targetLoaded, "munoExchangeTargetLoaded", false);
            Scribe_Values.Look(ref failReason, "munoExchangeFailReason");
        }

        /// <summary>
        /// 在目标被穿梭机成功带离地图后发放固定缪诺成员奖励。
        /// </summary>
        private void GrantReward()
        {
            if (rewardGranted || map == null)
            {
                return;
            }

            Pawn rewardPawn;
            if (!MunoHostageExchangeService.TryGenerateRewardPawn(out rewardPawn, out string generateFailReason))
            {
                MarkFailed(generateFailReason ?? "未能生成缪诺奖励成员，本次流程已中止。");
                return;
            }

            if (!MunoHostageExchangeService.TryAddRewardPawnToMap(map, rewardPawn, out string placeFailReason))
            {
                if (rewardPawn != null && !rewardPawn.Destroyed)
                {
                    rewardPawn.Destroy();
                }

                MarkFailed(placeFailReason ?? "未能让缪诺奖励成员加入殖民地，本次流程已中止。");
                return;
            }

            rewardGranted = true;
            finished = true;
            Messages.Message("缪诺已成功接收目标，一名新的缪诺成员已加入殖民地。", rewardPawn, MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// 在唯一目标完成装载后立即强制穿梭机离场，避免外部模组修改等待 Job 后无法自动进入起飞阶段。
        /// </summary>
        private bool TryLaunchShuttleNow()
        {
            CompShuttle shuttleComp = shuttle.TryGetComp<CompShuttle>();
            if (shuttleComp?.shipParent == null)
            {
                return false;
            }

            TransportShip shipParent = shuttleComp.shipParent;
            shipParent.ForceJob(ShipJobDefOf.FlyAway);
            return true;
        }
    }
}
