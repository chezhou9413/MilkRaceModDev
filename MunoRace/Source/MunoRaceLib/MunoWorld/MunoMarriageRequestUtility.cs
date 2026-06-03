using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责判断缪诺主动和亲请求的触发条件、发送请求信件并处理超时拒绝。
    /// </summary>
    public static class MunoMarriageRequestUtility
    {
        private const int RequiredGoodwill = 50;
        private const int RequestDurationTicks = GenDate.TicksPerDay * 2;
        private const string RequiredTitleDefName = "Muno_JuniorCollaborator";

        /// <summary>
        /// 在上帝模式调试入口中强制刷新一条当前可处理的和亲请求。
        /// </summary>
        public static void DebugForceRefreshRequest(MunoMarriageDiplomacyComponent component)
        {
            if (component == null)
            {
                return;
            }

            MunoMarriageCandidateUtility.CleanupGeneratedCandidates(component.Candidates);
            component.ClearCandidates();
            component.StartPendingRequest(MunoMarriageDiplomacyService.CurrentYear(), GenTicks.TicksAbs + RequestDurationTicks);
            Messages.Message("已刷新一条缪诺和亲请求。", MessageTypeDefOf.TaskCompletion, historical: false);
        }

        /// <summary>
        /// 在存档组件定时 Tick 中推进主动请求状态。
        /// </summary>
        public static void TickRequestState(MunoMarriageDiplomacyComponent component)
        {
            if (component == null)
            {
                return;
            }

            if (component.RequestPending && GenTicks.TicksAbs >= component.RequestExpireTick)
            {
                RejectExpiredRequest(component);
                return;
            }

            TryTriggerRequest(component);
        }

        /// <summary>
        /// 判断并触发一条新的主动和亲请求。
        /// </summary>
        private static void TryTriggerRequest(MunoMarriageDiplomacyComponent component)
        {
            int year = MunoMarriageDiplomacyService.CurrentYear();
            if (!component.CanTriggerNewRequest(year))
            {
                return;
            }

            Faction munoFaction = Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
            if (munoFaction == null || munoFaction.HostileTo(Faction.OfPlayer) || munoFaction.GoodwillWith(Faction.OfPlayer) <= RequiredGoodwill)
            {
                return;
            }

            if (!AnyColonistHasRequiredTitle(munoFaction))
            {
                return;
            }

            int expireTick = GenTicks.TicksAbs + RequestDurationTicks;
            component.StartPendingRequest(year, expireTick);
            Find.LetterStack.ReceiveLetter(
                "缪诺和亲请求",
                MunoCommDialogueUtility.RandomRequestLetter().Replace("{DAYS}", "2"),
                LetterDefOf.PositiveEvent);
        }

        /// <summary>
        /// 判断任意玩家自由殖民者是否拥有足够高的缪诺头衔。
        /// </summary>
        private static bool AnyColonistHasRequiredTitle(Faction munoFaction)
        {
            RoyalTitleDef requiredTitle = DefDatabase<RoyalTitleDef>.GetNamedSilentFail(RequiredTitleDefName);
            if (requiredTitle == null)
            {
                return false;
            }

            List<Pawn> pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                RoyalTitleDef currentTitle = pawn?.royalty?.GetCurrentTitle(munoFaction);
                if (currentTitle != null && currentTitle.seniority >= requiredTitle.seniority)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 处理玩家未在期限内回复时的自动拒绝。
        /// </summary>
        private static void RejectExpiredRequest(MunoMarriageDiplomacyComponent component)
        {
            MunoMarriageCandidateUtility.CleanupGeneratedCandidates(component.Candidates);
            component.ClearCandidates();
            component.FinishPendingRequest();
            Find.LetterStack.ReceiveLetter(
                "缪诺和亲请求已过期",
                MunoCommDialogueUtility.RandomExpiredRejectReply(),
                LetterDefOf.NeutralEvent);
        }
    }
}
