using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //为单个任务维护批量接收人员、装载时限和经原版确认的穿梭机离场状态。
    public class MunoQuestShuttle : IExposable
    {
        private Thing shuttle;
        private TransportShip ship;
        private string signalTag;
        private bool launched;
        private bool departed;
        private bool landed;
        private bool unsatisfied;
        private int expiryTick;
        private int requiredCount;
        private List<Pawn> passengers = new List<Pawn>();

        //返回原版是否确认足量任务人员已装载并起飞。
        public bool Departed => departed;

        //返回本次接收的截止时间。
        public int ExpiryTick => expiryTick;

        //创建接收机并采用原版入场天降物落入任务基地。
        public void Start(Quest quest, Faction faction, Map map, List<Pawn> pawns, float days)
        {
            if (pawns.Count == 0) throw new InvalidOperationException("接收穿梭机必须有待交付人员。");
            passengers = new List<Pawn>(pawns);
            requiredCount = passengers.Count;
            if (!DropCellFinder.TryFindShipLandingArea(map, out IntVec3 cell, out _)
                && !CellFinder.TryFindRandomCellNear(map.Center, map, 40,
                    c => RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(c, map).Accepted, out cell))
                throw new InvalidOperationException("任务基地没有可供接收穿梭机降落的空地。");
            shuttle = QuestGen_Shuttle.GenerateShuttle(owningFaction: faction, requiredPawns: passengers,
                acceptColonists: false, onlyAcceptHealthy: false, hideControls: true);
            if (shuttle == null) throw new InvalidOperationException("接收穿梭机生成失败。");
            signalTag = "Quest" + quest.id + ".MunoPickup";
            shuttle.questTags = new List<string> { signalTag };
            ship = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle);
            ShipJob_WaitForever wait = (ShipJob_WaitForever)ShipJobMaker.MakeShipJob(ShipJobDefOf.WaitForever);
            wait.leaveImmediatelyWhenSatisfied = false;
            wait.showGizmos = false;
            ship.AddJob(wait);
            expiryTick = GenTicks.TicksGame + (int)(days * GenDate.TicksPerDay);
            Thing incoming = SkyfallerMaker.MakeSkyfaller(ThingDefOf.ShuttleIncoming, shuttle);
            GenSpawn.Spawn(incoming, cell, map);
            ship.Start();
            Find.LetterStack.ReceiveLetter("缪诺接收穿梭机", "接收穿梭机正在降落，需要接收 " + requiredCount
                + " 名任务人员。请在 " + days + " 天内完成装载。", LetterDefOf.NeutralEvent, new LookTargets(incoming));
        }

        //判断人员是否已经进入接收机，以便优先保留已装载的合格俘虏。
        public bool Contains(Pawn pawn)
        {
            return shuttle != null && !shuttle.Destroyed && shuttle.TryGetComp<CompTransporter>().innerContainer.Contains(pawn);
        }

        //起飞前按抓捕人数要求替换失效人员，并同步原版接收条件与搬运清单。
        public void UpdatePassengers(List<Pawn> pawns)
        {
            if (launched || departed || shuttle == null || shuttle.Destroyed || passengers.SequenceEqual(pawns)) return;
            passengers = new List<Pawn>(pawns);
            CompShuttle shuttleComp = shuttle.TryGetComp<CompShuttle>();
            shuttleComp.requiredPawns.Clear();
            shuttleComp.requiredPawns.AddRange(passengers);
            if (landed) SetLoadingList(shuttle.TryGetComp<CompTransporter>());
        }

        //落地后发出装载任务，检查超时并在装载完成后请求起飞。
        public bool Poll(Map map, out string reason)
        {
            reason = null;
            if (departed) return true;
            if (unsatisfied) reason = "穿梭机未接收到足量任务人员便离场。";
            else if (shuttle == null || shuttle.Destroyed) reason = "接收穿梭机已损毁。";
            else if (GenTicks.TicksGame >= expiryTick) reason = "接收穿梭机的装载期限已过。";
            else if (shuttle.MapHeld != map) reason = "接收穿梭机意外离开任务基地。";
            if (reason != null) return false;
            if (!shuttle.Spawned) return true;
            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            if (!landed)
            {
                TransporterUtility.InitiateLoading(new[] { transporter });
                SetLoadingList(transporter);
                landed = true;
            }
            if (!launched && AllPassengersLoaded())
            {
                launched = true;
                ship.ForceJob(ShipJobDefOf.FlyAway);
            }
            return true;
        }

        //只为尚未进入容器的当前接收人员安排搬运，避免已经装载者被重复计数。
        private void SetLoadingList(CompTransporter transporter)
        {
            transporter.leftToLoad?.Clear();
            foreach (Pawn pawn in passengers)
            {
                if (transporter.innerContainer.Contains(pawn)) continue;
                TransferableOneWay transfer = new TransferableOneWay();
                transfer.things.Add(pawn);
                transporter.AddToTheToLoadList(transfer, 1);
            }
        }

        //同时验证人数、生命状态和实际持有链，禁止空机或少装人员结算成功。
        private bool AllPassengersLoaded()
        {
            return passengers.Count == requiredCount && requiredCount > 0
                && passengers.All(pawn => pawn != null && !pawn.Dead && !pawn.Destroyed && Contains(pawn));
        }

        //核对起飞信号所属穿梭机和实际容器，避免以物体消失代替成功。
        public void ReceiveSignal(Signal signal)
        {
            if (signal.tag == signalTag + ".SentUnsatisfied") unsatisfied = true;
            if (signal.tag == signalTag + ".SentSatisfied")
            {
                departed = AllPassengersLoaded();
                unsatisfied = !departed;
            }
        }

        //任务失败时卸下当前容器内的人物和物品，并安排空机离场。
        public void Cancel(Map map)
        {
            if (departed || shuttle == null || shuttle.Destroyed || map == null) return;
            CompTransporter transporter = shuttle.TryGetComp<CompTransporter>();
            transporter.CancelLoad(map);
            transporter.innerContainer.TryDropAll(shuttle.PositionHeld, map, ThingPlaceMode.Near);
            shuttle.TryGetComp<CompShuttle>().requiredPawns.Clear();
            ship?.ForceJob(ShipJobDefOf.FlyAway);
        }

        //保存机体、运输船与起飞确认状态，机体内容由原版持有链保存。
        public void ExposeData()
        {
            Scribe_References.Look(ref shuttle, "shuttle");
            Scribe_References.Look(ref ship, "ship");
            Scribe_Values.Look(ref signalTag, "signalTag");
            Scribe_Values.Look(ref launched, "launched");
            Scribe_Values.Look(ref departed, "departed");
            Scribe_Values.Look(ref landed, "landed");
            Scribe_Values.Look(ref unsatisfied, "unsatisfied");
            Scribe_Values.Look(ref expiryTick, "expiryTick");
            Scribe_Values.Look(ref requiredCount, "requiredCount");
            Scribe_Collections.Look(ref passengers, "passengers", LookMode.Reference);
        }
    }
}
