using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //追踪哨站指定目标从生成、活捉、带回基地到穿梭机交付的全过程。
    public class QuestPart_MunoCapture : QuestPart_MunoMilitary
    {
        private Site site;
        private bool captured;
        private bool returned;
        private bool handedOver;

        //在任务目标列表中增加哨站的世界跳转位置。
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo target in base.QuestLookTargets) yield return target;
                if (site != null && site.Spawned) yield return site;
            }
        }

        //接受后创建原版强盗营地，并将一名成年目标交由附加哨站部件持有。
        protected override void StartMission()
        {
            if (!MunoQuestUtility.FindSiteTile(map, out PlanetTile tile)) throw new InvalidOperationException("基地附近没有可达的空闲哨站地块。");
            float points = Mathf.Max(200f, StorytellerUtility.DefaultThreatPointsNow(map) * Config.threatFactor);
            SitePartDef targetDef = DefDatabase<SitePartDef>.GetNamed("Muno_CaptureTarget");
            site = SiteMaker.MakeSite(new[] { SitePartDefOf.BanditCamp, targetDef }, tile, enemy, threatPoints: points);
            PawnKindDef kind = enemy.def.pawnGroupMakers.Where(g => g.kindDef == PawnGroupKindDefOf.Combat)
                .SelectMany(g => g.options).Where(o => o.kind.RaceProps.Humanlike).RandomElementByWeight(o => o.selectionWeight).kind;
            subject = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, enemy, tile: tile,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, mustBeCapableOfViolence: true,
                fixedBiologicalAge: Rand.Range(20, 45), developmentalStages: DevelopmentalStage.Adult));
            SitePart targetPart = site.parts.Single(p => p.def == targetDef);
            targetPart.things = new ThingOwner<Pawn>(targetPart, oneStackOnly: true);
            if (!targetPart.things.TryAdd(subject)) throw new InvalidOperationException("无法将抓捕目标放入哨站持有容器。");
            subject.questTags = subject.questTags ?? new List<string>();
            subject.questTags.Add("Quest" + quest.id + ".MunoCaptureTarget");
            Find.WorldObjects.Add(site);
            dueTick = GenTicks.TicksGame + (int)(Config.captureDays * GenDate.TicksPerDay);
            progress = "前往标记哨站活捉指定目标，在期限内将其作为囚犯带回任意地面基地。";
            Find.LetterStack.ReceiveLetter("缪诺抓捕目标已定位", progress + "\n目标：" + subject.LabelShortCap,
                LetterDefOf.NeutralEvent, new LookTargets(site));
        }

        //检查抓捕身份与基地返回事件，并在返回后开始随机接收倒计时。
        protected override void TickMission()
        {
            if (handedOver) return;
            if (subject.IsPrisonerOfColony) captured = true;
            else if (captured) { Fail("抓捕目标已被释放、招募或失去玩家囚犯身份。"); return; }
            if (!returned)
            {
                if (GenTicks.TicksGame >= dueTick) { Fail("未在规定时间内将目标活捉并带回基地。"); return; }
                if (captured && MunoQuestUtility.ValidHome(subject.MapHeld))
                {
                    returned = true;
                    map = subject.MapHeld;
                    dueTick = GenTicks.TicksGame + (int)(Config.arrivalDays.RandomInRange * GenDate.TicksPerDay);
                    progress = "目标已带回基地，等待缪诺接收穿梭机。";
                    Find.LetterStack.ReceiveLetter("缪诺已安排接收", progress + "\n预计等待："
                        + (dueTick - GenTicks.TicksGame).ToStringTicksToPeriod(), LetterDefOf.NeutralEvent, subject);
                }
                else if (!captured && (site == null || !site.Spawned ||
                    (subject.ParentHolder != site.parts.Single(p => p.def.defName == "Muno_CaptureTarget") && subject.MapHeld != site.Map)))
                    Fail("指定目标已离开哨站或哨站已丢失。");
                return;
            }
            if (subject.MapHeld != map) { Fail("目标已离开锁定的交接基地。"); return; }
            if (pickup == null && GenTicks.TicksGame >= dueTick) StartPickup();
        }

        //记录人物完成交接，之后不再将其离开地图视为失败。
        protected override void OnPickupCompleted()
        {
            handedOver = true;
            base.OnPickupCompleted();
        }

        //通过正式接收逻辑提前触发已进入等待阶段的穿梭机。
        public void DebugCallShuttle()
        {
            if (quest.State != QuestState.Ongoing || !returned || pickup != null || rewardReady)
            { MunoQuestUtility.Reject("该抓捕任务尚未进入等待接收穿梭机阶段。"); return; }
            if (subject == null || subject.Dead || !subject.IsPrisonerOfColony || subject.MapHeld != map)
            { MunoQuestUtility.Reject("指定目标必须仍作为囚犯留在交接基地。"); return; }
            try { StartPickup(); }
            catch (Exception error) { FailException(error); }
        }

        //结束任务后只移除未生成地图的任务哨站，不删除远征队所在地图。
        public override void Cleanup()
        {
            base.Cleanup();
            if (site != null && !site.HasMap && !site.Destroyed) site.Destroy();
        }

        //保存哨站引用与抓捕、返营和交付阶段。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "site");
            Scribe_Values.Look(ref captured, "captured");
            Scribe_Values.Look(ref returned, "returned");
            Scribe_Values.Look(ref handedOver, "handedOver");
        }
    }
}
