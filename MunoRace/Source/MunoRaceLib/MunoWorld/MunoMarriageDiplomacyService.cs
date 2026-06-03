using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责处理缪诺和亲候选生成、接受拒绝、穿梭机送达和外交后续事件。
    /// </summary>
    public static class MunoMarriageDiplomacyService
    {
        private const int CandidateCount = 3;
        private const int AcceptGoodwillImpact = 80;
        private const int AbuseGoodwillImpact = -60;
        private const int MarriageGoodwillImpact = 20;
        private static readonly IntRange DeliveryDelayTicks = new IntRange(GenDate.TicksPerDay, GenDate.TicksPerDay * 3);

        /// <summary>
        /// 返回当前游戏中的和亲外交存档组件。
        /// </summary>
        public static MunoMarriageDiplomacyComponent Component()
        {
            return Current.Game.GetComponent<MunoMarriageDiplomacyComponent>();
        }

        /// <summary>
        /// 返回当前绝对年份，用于限制每年最多刷新一次和亲请求。
        /// </summary>
        public static int CurrentYear()
        {
            return GenDate.Year(GenTicks.TicksAbs, 0f);
        }

        /// <summary>
        /// 尝试取得当前候选人；只有主动请求有效时才会生成候选。
        /// </summary>
        public static bool TryGetOrCreateCandidates(out List<MunoMarriageCandidateRecord> candidates, out string failReason)
        {
            failReason = null;
            MunoMarriageDiplomacyComponent component = Component();
            int year = CurrentYear();
            if (!component.CanUseRequestThisYear(year))
            {
                candidates = null;
                failReason = MunoCommDialogueUtility.RandomNoRequestReply();
                return false;
            }

            if (component.HasCandidates())
            {
                candidates = component.Candidates;
                return true;
            }

            if (MunoDefDataRef.MunoRace_MarriagePrincess == null)
            {
                candidates = null;
                failReason = "缪诺和亲公主模板缺失。";
                component.FinishPendingRequest();
                return false;
            }

            List<MunoMarriageCandidateRecord> generatedCandidates = new List<MunoMarriageCandidateRecord>();
            for (int i = 0; i < CandidateCount; i++)
            {
                Pawn pawn = MunoMarriageCandidateUtility.GeneratePrincessPawn();
                if (pawn == null)
                {
                    MunoMarriageCandidateUtility.CleanupGeneratedCandidates(generatedCandidates);
                    candidates = null;
                    failReason = "未能生成和亲候选人。";
                    component.FinishPendingRequest();
                    return false;
                }

                if (!pawn.IsWorldPawn())
                {
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                }

                generatedCandidates.Add(new MunoMarriageCandidateRecord
                {
                    pawn = pawn
                });
            }

            component.SetCandidates(generatedCandidates);
            candidates = component.Candidates;
            return true;
        }

        /// <summary>
        /// 接受指定和亲公主并安排延迟送达流程。
        /// </summary>
        public static bool TryAcceptPrincess(Pawn negotiator, Pawn princess, out string failReason)
        {
            failReason = null;
            if (negotiator?.Map == null)
            {
                failReason = "当前谈判者不在有效地图中，无法安排和亲穿梭机。";
                return false;
            }

            MunoMarriageDiplomacyComponent component = Component();
            if (!component.HasActiveRequest())
            {
                failReason = "当前没有有效的和亲请求。";
                return false;
            }

            MunoMarriageCandidateRecord record = component.FindRecord(princess);
            if (record == null || !component.Candidates.Contains(record))
            {
                failReason = "所选和亲候选人已经失效。";
                return false;
            }

            List<Thing> dowry = MunoMarriageDowryUtility.GenerateRandomDowry();
            if (dowry == null || dowry.Count == 0)
            {
                failReason = "缪诺嫁妆清单生成失败，本次和亲安排被取消。";
                component.FinishPendingRequest();
                return false;
            }

            record.accepted = true;
            component.RegisterAcceptedPrincess(princess);
            component.ClearCandidates();
            int deliveryTick = DebugSettings.godMode ? GenTicks.TicksAbs : GenTicks.TicksAbs + DeliveryDelayTicks.RandomInRange;
            component.ScheduleDelivery(princess, negotiator.Map, dowry, deliveryTick);
            if (DebugSettings.godMode)
            {
                component.TryStartScheduledDelivery();
            }

            ApplyAcceptGoodwill(princess);
            return true;
        }

        /// <summary>
        /// 拒绝本次和亲请求并清理当前候选人。
        /// </summary>
        public static void RejectCurrentRequest()
        {
            MunoMarriageDiplomacyComponent component = Component();
            MunoMarriageCandidateUtility.CleanupGeneratedCandidates(component.Candidates);
            component.ClearCandidates();
            component.FinishPendingRequest();
        }

        /// <summary>
        /// 将抵达地图的和亲公主整理为玩家可控制殖民者。
        /// </summary>
        public static void NormalizePrincessAfterArrival(Pawn princess)
        {
            if (princess == null)
            {
                return;
            }

            RecruitUtility.Recruit(princess, Faction.OfPlayer);
            princess.guest?.SetGuestStatus(null);
            if (princess.playerSettings == null)
            {
                princess.playerSettings = new Pawn_PlayerSettings(princess);
            }

            princess.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
            Messages.Message("缪诺和亲公主 " + princess.LabelShort + " 已抵达殖民地。", princess, MessageTypeDefOf.TaskCompletion, historical: false);
            Find.LetterStack.ReceiveLetter(
                "缪诺和亲公主抵达",
                MunoCommDialogueUtility.RandomDeliveryReply().Replace("{PAWN}", princess.LabelShort),
                LetterDefOf.PositiveEvent,
                princess);
        }

        /// <summary>
        /// 对接受和亲应用一次性好感与首次自然好感奖励。
        /// </summary>
        private static void ApplyAcceptGoodwill(Pawn princess)
        {
            MunoMarriageDiplomacyComponent component = Component();
            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            munoFaction?.TryAffectGoodwillWith(Faction.OfPlayer, AcceptGoodwillImpact, canSendMessage: true, canSendHostilityLetter: true, reason: null, lookTarget: princess);
            component.ActivateNaturalGoodwillReward();
        }

        /// <summary>
        /// 在公主被贩卖或摘器官致死时应用负面外交后续。
        /// </summary>
        public static void TryApplyAbusePenalty(Pawn princess, string reason)
        {
            MunoMarriageCandidateRecord record = Component().FindRecord(princess);
            if (record == null || !record.accepted || record.abusePenaltyGiven)
            {
                return;
            }

            record.abusePenaltyGiven = true;
            Component().ActivateNaturalGoodwillPenalty();
            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            munoFaction?.TryAffectGoodwillWith(Faction.OfPlayer, AbuseGoodwillImpact, canSendMessage: true, canSendHostilityLetter: true, reason: null, lookTarget: princess);
            Messages.Message("缪诺得知和亲公主遭遇不幸：" + reason, princess, MessageTypeDefOf.NegativeEvent, historical: false);
        }

        /// <summary>
        /// 在公主自然结婚时应用正面外交后续。
        /// </summary>
        public static void TryApplyMarriageReward(Pawn princess, Pawn spouse)
        {
            MunoMarriageCandidateRecord record = Component().FindRecord(princess);
            if (record == null || !record.accepted || record.marriageRewardGiven)
            {
                return;
            }

            if (spouse == null || spouse.Faction != Faction.OfPlayer)
            {
                return;
            }

            record.marriageRewardGiven = true;
            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            munoFaction?.TryAffectGoodwillWith(Faction.OfPlayer, MarriageGoodwillImpact, canSendMessage: true, canSendHostilityLetter: true, reason: null, lookTarget: princess);
            Find.LetterStack.ReceiveLetter(
                "缪诺和亲公主融入殖民地",
                "缪诺方面发来讯息：她们很高兴和亲公主 " + princess.LabelShort + " 能够真正融入你的殖民地，并与 " + spouse.LabelShort + " 建立家庭。她们认为这证明双方关系正在走向稳定。",
                LetterDefOf.PositiveEvent,
                princess);
        }

        /// <summary>
        /// 判断指定 Pawn 是否是仍在追踪中的和亲公主。
        /// </summary>
        public static bool IsTrackedPrincess(Pawn pawn)
        {
            MunoMarriageCandidateRecord record = Component().FindRecord(pawn);
            return record != null && record.accepted;
        }

    }
}
