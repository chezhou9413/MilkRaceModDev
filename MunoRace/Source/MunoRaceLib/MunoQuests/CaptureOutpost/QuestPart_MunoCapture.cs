using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //按数量追踪据点俘虏的返营与交付，并记录带回情报的附加目标。
    public class QuestPart_MunoCapture : QuestPart_MunoMilitary
    {
        private Site site;
        private MunoCaptureRoster roster = new MunoCaptureRoster();
        private int captureDeadline;
        private bool returned;
        private bool handedOver;

        //返回据点引用，供地图生成部件寻找所属任务。
        public Site Site => site;

        //抓捕任务只检查人数，不要求某一名指定人物存活。
        protected override bool RequiresSubject => false;

        //情报附加奖励独立于成员和普通物资二选一。
        public override float BonusRewardValue => roster.IntelligenceRecovered ? Config.intelligenceRewardValue : 0f;

        //显示当前基地可交付人数及情报收集状态。
        public override string DescriptionPart => progress + "\n抓捕要求：" + Config.captureCount
            + " 名本据点的任意成年敌人。\n" + (handedOver ? "已交付俘虏：" : "当前基地合格俘虏：")
            + (handedOver ? Config.captureCount : roster.AvailableAt(map).Count)
            + " 名。\n情报附加奖励：" + (roster.IntelligenceRecovered ? "已取得" : "尚未取得（可选）");

        //在任务目标列表中增加据点的世界位置。
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo target in base.QuestLookTargets) yield return target;
                if (site != null && site.Spawned) yield return site;
            }
        }

        //保留本任务守军引用，避免运输中的俘虏被世界人物回收。
        public override bool QuestPartReserves(Pawn pawn)
        {
            return !settled && roster.Contains(pawn);
        }

        //标记合格俘虏所属的数量型任务，不指定唯一目标。
        public override string ExtraInspectString(ISelectable target)
        {
            return !settled && target is Pawn pawn && roster.Contains(pawn)
                ? "缪诺据点抓捕任务：带回任意 " + Config.captureCount + " 名成年守军即可。" : null;
        }

        //接受时创建敌对据点，并只抽取一次是否携带情报。
        protected override void StartMission()
        {
            if (Config.captureCount < 1) throw new InvalidOperationException("抓捕要求人数必须大于零。");
            if (!MunoQuestUtility.FindSiteTile(map, out PlanetTile tile))
                throw new InvalidOperationException("基地附近没有可达的空闲据点地块。");
            float points = Mathf.Max(200f, StorytellerUtility.DefaultThreatPointsNow(map) * Config.threatFactor);
            SitePartDef rosterDef = DefDatabase<SitePartDef>.GetNamed("Muno_CaptureOutpostRoster");
            site = SiteMaker.MakeSite(new[] { SitePartDefOf.BanditCamp, rosterDef }, tile, enemy, threatPoints: points);
            SitePart rosterPart = site.parts.Single(part => part.def == rosterDef);
            if (Rand.Chance(Config.intelligenceChance))
            {
                Thing intelligence = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Muno_OutpostIntelligence"));
                intelligence.TryGetComp<CompUseEffect_MunoOutpostIntel>().Initialize(enemy);
                rosterPart.things = new ThingOwner<Thing>(rosterPart, false);
                if (!rosterPart.things.TryAdd(intelligence)) throw new InvalidOperationException("无法保存据点情报。");
                roster.SetIntelligence(intelligence);
            }
            Find.WorldObjects.Add(site);
            captureDeadline = GenTicks.TicksGame + (int)(Config.captureDays * GenDate.TicksPerDay);
            dueTick = captureDeadline;
            progress = "前往标记据点，活捉任意 " + Config.captureCount + " 名成年守军，在期限内带回同一座地面基地。";
            Find.LetterStack.ReceiveLetter("缪诺抓捕据点已定位", progress
                + "\n若发现据点情报，在俘虏交付前带回基地，可额外获得约 " + Config.intelligenceRewardValue
                + " 银币物资。", LetterDefOf.NeutralEvent, new LookTargets(site));
        }

        //由据点部件登记实际守军，失败时明确终止当前任务。
        public void RegisterOutpost(SitePart part)
        {
            try
            {
                roster.RegisterDefenders(site, part, Config.captureCount);
                Find.LetterStack.ReceiveLetter("缪诺据点抓捕要求", "活捉并带回任意 " + Config.captureCount
                    + " 名本据点成年守军即可，无需保护某一名指定目标。留意敌人携带的据点情报。",
                    LetterDefOf.NeutralEvent, new LookTargets(site));
            }
            catch (Exception error) { FailException(error); }
        }

        //按实际可交付人数推进返营与接收，起飞前允许替换死亡或失去囚犯身份的人员。
        protected override void TickMission()
        {
            if (handedOver || rewardReady) return;
            roster.CheckIntelligenceReturned();
            if (pickup != null)
            {
                List<Pawn> passengers = roster.AvailableAt(map).OrderByDescending(pawn => pickup.Contains(pawn))
                    .ThenBy(pawn => pawn.thingIDNumber).Take(Config.captureCount).ToList();
                pickup.UpdatePassengers(passengers);
                return;
            }
            if (!returned)
            {
                if (GenTicks.TicksGame >= captureDeadline) { Fail("未在规定时间内带回足量据点俘虏。"); return; }
                Map home = Find.Maps.FirstOrDefault(candidate => MunoQuestUtility.ValidHome(candidate)
                    && roster.AvailableAt(candidate).Count >= Config.captureCount);
                if (home != null)
                {
                    map = home;
                    returned = true;
                    dueTick = GenTicks.TicksGame + (int)(Config.arrivalDays.RandomInRange * GenDate.TicksPerDay);
                    progress = "已带回足量俘虏，等待缪诺接收穿梭机。";
                    Find.LetterStack.ReceiveLetter("缪诺已安排接收", progress + "\n预计等待："
                        + (dueTick - GenTicks.TicksGame).ToStringTicksToPeriod(), LetterDefOf.NeutralEvent, new LookTargets(map.Parent));
                }
                else if (roster.Registered && !roster.HasEnoughPossibleTargets(site, Config.captureCount))
                    Fail("据点剩余守军和已俘人员的总数不足，无法满足抓捕数量。");
                else if (!roster.Registered && (site == null || !site.Spawned))
                    Fail("抓捕据点已丢失。");
                return;
            }
            List<Pawn> available = roster.AvailableAt(map);
            if (available.Count < Config.captureCount)
            {
                returned = false;
                dueTick = captureDeadline;
                progress = "基地内合格俘虏人数不足，请在抓捕期限内补足人数。";
                return;
            }
            if (GenTicks.TicksGame >= dueTick) StartPickup(available.Take(Config.captureCount).ToList());
        }

        //在穿梭机确认交付后锁定情报结果，再生成唯一奖励候选。
        protected override void OnPickupCompleted()
        {
            roster.CheckIntelligenceReturned();
            handedOver = true;
            base.OnPickupCompleted();
        }

        //在使用情报的同一帧补记带回基地，交付后不再改变已展示的奖励。
        public void NotifyIntelligenceUsed(Thing item, Map usedMap)
        {
            if (!settled && !rewardReady) roster.NotifyIntelligenceUsed(item, usedMap);
        }

        //通过正式接收逻辑提前触发人数已经满足的穿梭机。
        public void DebugCallShuttle()
        {
            if (quest.State != QuestState.Ongoing || !returned || pickup != null || rewardReady)
            { MunoQuestUtility.Reject("该抓捕任务尚未进入等待接收穿梭机阶段。"); return; }
            List<Pawn> available = roster.AvailableAt(map);
            if (available.Count < Config.captureCount)
            { MunoQuestUtility.Reject("交接基地内的合格据点俘虏人数不足。"); return; }
            try { StartPickup(available.Take(Config.captureCount).ToList()); }
            catch (Exception error) { FailException(error); }
        }

        //仅删除没有玩家地图的任务据点，保留远征队仍在使用的地图。
        public override void Cleanup()
        {
            base.Cleanup();
            if (site != null && !site.HasMap && !site.Destroyed) site.Destroy();
        }

        //保存据点、守军名册、原始期限及数量交接阶段。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
            Scribe_Deep.Look(ref roster, "roster");
            Scribe_Values.Look(ref captureDeadline, "captureDeadline");
            Scribe_Values.Look(ref returned, "returned");
            Scribe_Values.Look(ref handedOver, "handedOver");
        }
    }
}
