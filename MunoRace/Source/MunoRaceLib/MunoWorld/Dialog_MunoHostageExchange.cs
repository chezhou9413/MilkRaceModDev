using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责管理缪诺人口交换通讯窗口的页面、上下文和提交动作。
    [StaticConstructorOnStartup]
    public class Dialog_MunoHostageExchange : MunoWindowBase
    {
        private const float ManagerColumnWidth = 280f;
        private const float ManagerColumnGap = 14f;
        private const float ManagerPortraitScale = 1.033f;
        private const float ManagerPortraitOffsetX = -16.19f;
        private const float ManagerPortraitVerticalFactor = 0.4f;
        private static readonly Texture2D MunoLogo = ContentFinder<Texture2D>.Get("UI/MunoLogo", true);
        private static readonly Texture2D ManagerPortrait = ContentFinder<Texture2D>.Get("UI/MunoCommPortrait", true);

        private readonly Pawn negotiator;
        private readonly Map map;
        private readonly Settlement settlement;
        private readonly Caravan caravan;
        private readonly bool caravanMode;
        private MunoHostageExchangeDraft draft = new MunoHostageExchangeDraft();
        private CommPage currentPage;
        private Vector2 targetScrollPosition;
        private Vector2 rewardScrollPosition;
        private Vector2 previewScrollPosition;
        private float pageOpenTime;
        private MunoTypewriterTextState typewriter = new MunoTypewriterTextState();

        //表示通讯窗口当前显示的交换页面。
        private enum CommPage
        {
            MainMenu,
            Targets,
            Rewards
        }

        //构建绑定当前地图谈判者的人口交换窗口。
        public Dialog_MunoHostageExchange(Pawn negotiator)
        {
            this.negotiator = negotiator;
            map = negotiator?.Map;
            pageOpenTime = Time.realtimeSinceStartup;
            typewriter.SetText(MunoCommDialogueUtility.RandomGreeting());
        }

        //构建人口交换窗口，并按需要直接进入候选选择页。
        public Dialog_MunoHostageExchange(Pawn negotiator, bool startAtExchange) : this(negotiator)
        {
            if (startAtExchange)
            {
                SetPage(CommPage.Targets);
            }
        }

        //构建绑定缪诺据点远行队的批量交换窗口。
        public Dialog_MunoHostageExchange(Settlement settlement, Caravan caravan)
        {
            this.settlement = settlement;
            this.caravan = caravan;
            caravanMode = true;
            List<Pawn> pawns = caravan?.PawnsListForReading;
            negotiator = pawns != null && pawns.Count > 0 ? pawns[0] : null;
            pageOpenTime = Time.realtimeSinceStartup;
            typewriter.SetText(MunoCommDialogueUtility.RandomGreeting());
        }

        //返回缪诺人口交换通讯窗口的固定初始尺寸。
        public override Vector2 InitialSize => new Vector2(1024f, 680f);

        //在窗口通过外部关闭方式结束时清理尚未提交的物资预览。
        public override void PreClose()
        {
            draft?.DisposePreview();
            base.PreClose();
        }

        //绘制首页或批量交换页面，并处理窗口关闭时的预览清理。
        public override void DoWindowContents(Rect inRect)
        {
            if (currentPage == CommPage.MainMenu)
            {
                MunoCommUIStyle.DrawBackground(inRect);
                if (MunoCommUIStyle.DrawTerminalHeader(inRect, "缪诺人口交换", MunoLogo))
                {
                    CloseWithDraftCleanup();
                    return;
                }

                DrawMainMenuPage(new Rect(inRect.x, inRect.y + 54f, inRect.width, inRect.height - 54f));
                return;
            }

            MunoCommUIStyle.DrawBackground(inRect);
            if (MunoCommUIStyle.DrawTerminalHeader(inRect, "缪诺人口交换", MunoLogo))
            {
                CloseWithDraftCleanup();
                return;
            }

            DrawExchangePage(new Rect(inRect.x + 14f, inRect.y + 56f, inRect.width - 28f, inRect.height - 70f));
        }

        //绘制通讯首页，并在人口交换入口被点击后进入交换页面。
        private void DrawMainMenuPage(Rect rect)
        {
            MunoCommMainMenuAction action = MunoCommMainMenuView.Draw(rect, pageOpenTime, typewriter);
            if (action == MunoCommMainMenuAction.OpenPopulationExchange)
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                SetPage(CommPage.Targets);
                return;
            }

            if (action == MunoCommMainMenuAction.Close)
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                CloseWithDraftCleanup();
            }
        }

        //在问候文本未播放完成时展开全文，并返回是否拦截了当前操作。
        private bool CompleteTextIfNeeded()
        {
            if (typewriter.Completed)
            {
                return false;
            }

            typewriter.Complete();
            return true;
        }

        //绘制交换说明、进行中状态和当前页面内容。
        private void DrawExchangePage(Rect rect)
        {
            Rect managerRect = new Rect(rect.x, rect.y, ManagerColumnWidth, rect.height);
            Rect workspaceRect = new Rect(managerRect.xMax + ManagerColumnGap, rect.y, rect.width - ManagerColumnWidth - ManagerColumnGap, rect.height);
            DrawMilitaryManagerPanel(managerRect);

            float statusHeight = CalculateStatusPanelHeight(workspaceRect.width);
            Rect statusRect = new Rect(workspaceRect.x, workspaceRect.y, workspaceRect.width, statusHeight);
            Rect contentRect = new Rect(workspaceRect.x, statusRect.yMax + 10f, workspaceRect.width, workspaceRect.height - statusRect.height - 10f);
            DrawStatusPanel(statusRect);

            List<Pawn> candidates = GetCandidates();
            draft.SyncCandidates(candidates);
            bool exchangeBlocked = !caravanMode && MunoShuttleExchangeService.CurrentSession().HasActiveSession;
            string blockedReason = exchangeBlocked ? "已有缪诺接收穿梭机正在执行任务。" : null;
            if (caravanMode && CannotCarryItemRewardsAfterExchange())
            {
                exchangeBlocked = true;
                blockedReason = "当前选择会让远行队没有成员承载物资奖励；请保留一名未上交成员或将至少一名目标设为缪诺成员。";
            }
            MunoHostageExchangePanelAction action;
            if (currentPage == CommPage.Targets)
            {
                action = MunoHostageExchangePanelView.DrawTargetPage(contentRect, candidates, draft, ref targetScrollPosition, caravanMode);
            }
            else
            {
                action = MunoHostageExchangePanelView.DrawRewardPage(contentRect, draft, ref rewardScrollPosition, ref previewScrollPosition, caravanMode, exchangeBlocked, blockedReason);
            }

            if (action == MunoHostageExchangePanelAction.OpenRewards)
            {
                SetPage(CommPage.Rewards);
            }
            else if (action == MunoHostageExchangePanelAction.BackToTargets)
            {
                SetPage(CommPage.Targets);
            }
            else if (action == MunoHostageExchangePanelAction.Submit)
            {
                TrySubmitExchange();
            }
        }

        //绘制军事管理员立绘栏和身份名称。
        private static void DrawMilitaryManagerPanel(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(10f);
            float labelHeight = Text.LineHeightOf(GameFont.Medium) + 12f;
            Rect portraitRect = new Rect(inner.x, inner.y, inner.width, inner.height - labelHeight - 10f);
            Rect labelRect = new Rect(inner.x, portraitRect.yMax + 10f, inner.width, labelHeight);

            MunoCommUIStyle.DrawLightPanel(portraitRect);
            DrawMilitaryManagerPortrait(portraitRect.ContractedBy(4f));
            MunoCommUIStyle.DrawLightPanel(labelRect);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;
                GUI.color = MunoCommUIStyle.DarkTextColor;
                Widgets.Label(labelRect, "军事管理员");
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        //按旧通讯页面的裁切比例绘制军事管理员立绘。
        private static void DrawMilitaryManagerPortrait(Rect rect)
        {
            GUI.BeginGroup(rect);
            try
            {
                Rect localRect = new Rect(0f, 0f, rect.width, rect.height);
                Widgets.DrawBoxSolid(localRect, new Color(0.14f, 0.19f, 0.19f));
                float scale = Mathf.Max(rect.width / ManagerPortrait.width, rect.height / ManagerPortrait.height) * ManagerPortraitScale;
                float drawWidth = ManagerPortrait.width * scale;
                float drawHeight = ManagerPortrait.height * scale;
                float drawX = (rect.width - drawWidth) * 0.5f + ManagerPortraitOffsetX;
                float drawY = (rect.height - drawHeight) * ManagerPortraitVerticalFactor;
                GUI.DrawTexture(new Rect(drawX, drawY, drawWidth, drawHeight), ManagerPortrait, ScaleMode.StretchToFill, true);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        //绘制当前交换规则和已进行会话的状态。
        private void DrawStatusPanel(Rect rect)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect inner = rect.ContractedBy(10f);
            string text = BuildStatusPanelText();
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = MunoCommUIStyle.DarkTextColor;
                float height = Mathf.Ceil(Text.CalcHeight(text, inner.width)) + 2f;
                Widgets.Label(new Rect(inner.x, inner.y, inner.width, height), text);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        //根据当前上下文组合人口交换状态面板文本。
        private string BuildStatusPanelText()
        {
            string modeText = caravanMode ? "缪诺据点批量交换" : "缪诺接收穿梭机";
            string statusText = caravanMode ? "远行队交换会在确认后立即完成。" : BuildStatusText();
            return modeText + "\n每名上交人员对应一份奖励，可逐人选择缪诺成员或原版任务式等值物资。\n" + statusText;
        }

        //按照实际中文换行高度计算状态面板尺寸，避免覆盖下方交换列表。
        private float CalculateStatusPanelHeight(float width)
        {
            GameFont oldFont = Text.Font;
            bool oldWordWrap = Text.WordWrap;
            try
            {
                Text.Font = GameFont.Small;
                Text.WordWrap = true;
                float textWidth = Mathf.Max(1f, width - 20f);
                return Mathf.Ceil(Text.CalcHeight(BuildStatusPanelText(), textWidth)) + 22f;
            }
            finally
            {
                Text.Font = oldFont;
                Text.WordWrap = oldWordWrap;
            }
        }

        //返回当前上下文内的可交换候选列表。
        private List<Pawn> GetCandidates()
        {
            return caravanMode ? MunoHostageExchangeService.GetExchangeCandidates(caravan) : MunoHostageExchangeService.GetExchangeCandidates(map);
        }

        //判断纯物资奖励是否会导致据点交换后远行队没有可用载体。
        private bool CannotCarryItemRewardsAfterExchange()
        {
            if (draft.CountRewardType(MunoExchangeRewardType.RandomItems) == 0
                || draft.CountRewardType(MunoExchangeRewardType.MunoPawn) > 0
                || caravan == null)
            {
                return false;
            }

            List<Pawn> caravanPawns = caravan.PawnsListForReading;
            for (int i = 0; i < caravanPawns.Count; i++)
            {
                if (!draft.IsSelected(caravanPawns[i]))
                {
                    return false;
                }
            }

            return true;
        }

        //提交当前人员和奖励分配到据点或穿梭机服务。
        private void TrySubmitExchange()
        {
            if (!draft.EnsureItemPreview(out string previewFailReason))
            {
                Messages.Message(previewFailReason ?? "未能生成随机物资奖励。", MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<MunoExchangeTargetRecord> targets = draft.BuildTargetRecords();
            if (caravanMode)
            {
                if (MunoHostageExchangeService.TryExchangePawns(settlement, caravan, targets, draft.ItemRewards, out string failReason, out List<Pawn> joinedPawns))
                {
                    Messages.Message("已完成 " + targets.Count + " 名人员的缪诺批量交换，加入 " + joinedPawns.Count + " 名缪诺成员。", MessageTypeDefOf.PositiveEvent, false);
                    draft = new MunoHostageExchangeDraft();
                    Close();
                }
                else
                {
                    Messages.Message(failReason ?? "未能完成缪诺批量交换。", MessageTypeDefOf.RejectInput, false);
                    draft.DisposePreview();
                }

                return;
            }

            if (MunoShuttleExchangeService.TryStartExchange(negotiator, targets, draft.ItemRewards, out string shuttleFailReason))
            {
                Messages.Message("缪诺接收穿梭机已进场，请将选中的 " + targets.Count + " 名目标送入穿梭机。", MessageTypeDefOf.TaskCompletion, false);
                draft = new MunoHostageExchangeDraft();
                Close();
            }
            else
            {
                Messages.Message(shuttleFailReason ?? "未能发起缪诺接收流程。", MessageTypeDefOf.RejectInput, false);
                draft.DisposePreview();
            }
        }

        //组合当前地图穿梭机交换状态文本。
        private static string BuildStatusText()
        {
            MunoShuttleExchangeSession session = MunoShuttleExchangeService.CurrentSession();
            if (!session.HasActiveSession)
            {
                return "当前没有进行中的缪诺接收流程。";
            }

            return "进行中：已装载 " + session.LoadedTargetCount + " / " + session.TargetCount + " 名目标。";
        }

        //关闭窗口前销毁尚未交给交换会话的物资预览。
        private void CloseWithDraftCleanup()
        {
            draft?.DisposePreview();
            Close();
        }

        //切换页面并重置页面进入动画时间。
        private void SetPage(CommPage page)
        {
            currentPage = page;
            pageOpenTime = Time.realtimeSinceStartup;
        }
    }
}
