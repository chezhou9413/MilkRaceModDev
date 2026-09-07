using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MunoRaceLib.MunoQuests
{
    //管理缪诺伤员抵达、临时控制、追兵战斗与队员加入或撤离的任务状态。
    public class QuestPart_MunoProtect : QuestPart_MunoMilitary
    {
        private List<Pawn> raiders = new List<Pawn>();
        private bool raidStarted;
        private bool returning;
        private bool joined;

        //取得预先登记到原版任务缓存中的临时归属部件。
        private QuestPart_ExtraFaction LodgerPart => quest.PartsListForReading.OfType<QuestPart_ExtraFaction>().Single();

        //接受后生成伤员，以临时玩家人物身份通过运输舱抵达。
        protected override void StartMission()
        {
            if (!DropCellFinder.TryFindDropSpotNear(map.Center, map, out IntVec3 cell, allowFogged: false, canRoofPunch: false))
                throw new InvalidOperationException("没有可供伤员运输舱降落的空地。");
            subject = MunoRaiderUtility.Generate(giver, map, Config);
            LodgerPart.affectedPawns.Add(subject);
            subject.SetFaction(Faction.OfPlayer);
            DropPodUtility.DropThingsNear(cell, map, new Thing[] { subject }, 110, false, false, false, false, false, giver);
            dueTick = GenTicks.TicksGame + (int)(Config.arrivalDays.RandomInRange * GenDate.TicksPerDay);
            progress = "受伤突袭队员已抵达，请救治并保护她，等待追兵抵达。";
            Find.LetterStack.ReceiveLetter("缪诺伤员抵达", progress + "\n" + subject.LabelShortCap
                + "（射击 " + subject.skills.GetSkill(SkillDefOf.Shooting).Level + "，格斗 "
                + subject.skills.GetSkill(SkillDefOf.Melee).Level + "）\n追兵预计抵达："
                + (dueTick - GenTicks.TicksGame).ToStringTicksToPeriod(), LetterDefOf.NeutralEvent, subject);
        }

        //确认伤员没有被囚禁或送走，再按时生成并追踪本次追兵。
        protected override void TickMission()
        {
            if (subject.IsPrisoner || subject.IsSlave || subject.Faction != Faction.OfPlayer)
            { Fail("受保护队员已被囚禁、奴役或失去临时玩家控制。"); return; }
            if (subject.MapHeld != map) { Fail("受保护队员已离开任务基地。"); return; }
            if (returning || rewardReady) return;
            if (!raidStarted)
            {
                if (GenTicks.TicksGame >= dueTick) SpawnRaid();
                return;
            }
            if (raiders.All(p => p == null || p.Dead || p.Destroyed || p.Downed || p.IsPrisonerOfColony || p.MapHeld != map))
                OfferRewards();
        }

        //采用原版袭击人员生成和战斗指挥逻辑，并只记录本次生成的追兵。
        private void SpawnRaid()
        {
            if (enemy == null || enemy.defeated || !enemy.HostileTo(Faction.OfPlayer))
                throw new InvalidOperationException("追兵派系已失效或不再敌对。");
            IncidentParms parms = new IncidentParms
            {
                target = map,
                faction = enemy,
                points = Math.Max(enemy.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat),
                    StorytellerUtility.DefaultThreatPointsNow(map) * Config.threatFactor),
                forced = true,
                raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
                raidStrategy = RaidStrategyDefOf.ImmediateAttack,
                pawnGroupKind = PawnGroupKindDefOf.Combat,
                canKidnap = true,
                canSteal = false,
                canTimeoutOrFlee = true
            };
            IncidentWorker_Raid worker = (IncidentWorker_Raid)IncidentDefOf.RaidEnemy.Worker;
            if (!worker.TryGenerateRaidInfo(parms, out raiders)) throw new InvalidOperationException("缪诺保护任务的追兵生成失败。");
            parms.raidStrategy.Worker.MakeLords(parms, raiders);
            raidStarted = true;
            dueTick = 0;
            progress = "追兵已抵达。击退本次全部追兵，同时确保受保护队员存活并留在基地。";
            Find.LetterStack.ReceiveLetter("追击缪诺的敌人抵达", progress, LetterDefOf.ThreatBig, new LookTargets(raiders));
        }

        //领取前再次检查伤员生命、归属和地图，避免选择过期奖励。
        public override bool CanChooseReward(out string reason)
        {
            if (!base.CanChooseReward(out reason)) return false;
            if (returning) reason = "正在等待受保护队员完成撤离。";
            else if (subject == null || subject.Dead || subject.Destroyed || subject.IsPrisoner || subject.IsSlave
                || subject.Faction != Faction.OfPlayer || subject.MapHeld != map) reason = "受保护队员已不满足任务要求。";
            return reason == null;
        }

        //成员分支直接解除临时身份，物资分支先安排接收队员。
        public override void ChooseReward(bool takePawn)
        {
            if (!CanChooseReward(out string reason)) { MunoQuestUtility.Reject(reason); return; }
            try
            {
                if (takePawn)
                {
                    Rewards.Deliver(this, true);
                    joined = true;
                    LodgerPart.affectedPawns.Remove(subject);
                    Finish();
                }
                else
                {
                    returning = true;
                    StartPickup();
                    progress = "已选择物资奖励，请将受保护队员送上接收穿梭机；起飞后发放物资。";
                }
            }
            catch (Exception error) { FailException(error); }
        }

        //队员撤离后发放锁定物资并完成任务。
        protected override void OnPickupCompleted()
        {
            Rewards.Deliver(this, false);
            LodgerPart.affectedPawns.Remove(subject);
            if (subject != null && !subject.Destroyed) subject.SetFaction(giver);
            Finish();
        }

        //提前执行当前等待中的追兵事件，沿用正式生成逻辑。
        public void DebugSpawnRaid()
        {
            if (quest.State != QuestState.Ongoing || raidStarted || subject == null || subject.Dead || subject.MapHeld != map)
            { MunoQuestUtility.Reject("该保护任务尚未进入有效的等待追兵阶段。"); return; }
            try { SpawnRaid(); }
            catch (Exception error) { FailException(error); }
        }

        //失败时恢复仍在基地中的队员归属并允许其自行撤离，保留现有伤势与物品。
        public override void Cleanup()
        {
            base.Cleanup();
            LodgerPart.affectedPawns.Clear();
            if (!joined && subject != null && !subject.Destroyed && subject.Faction == Faction.OfPlayer)
            {
                subject.SetFaction(giver);
                if (subject.Spawned && !subject.Dead && !subject.IsPrisoner && !subject.IsSlave)
                {
                    subject.GetLord()?.Notify_PawnLost(subject, PawnLostCondition.ForcedToJoinOtherLord);
                    LordMaker.MakeNewLord(giver, new LordJob_ExitMapBest(LocomotionUrgency.Walk, canDefendSelf: true), subject.Map, new[] { subject });
                }
            }
        }

        //保存追兵列表、撤离选择和永久加入结果。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref raiders, "raiders", LookMode.Reference);
            Scribe_Values.Look(ref raidStarted, "raidStarted");
            Scribe_Values.Look(ref returning, "returning");
            Scribe_Values.Look(ref joined, "joined");
        }
    }
}
