using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责显示缪诺通讯台和亲流程界面。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Dialog_MunoMarriageComms : MunoWindowBase
    {
        private const float LeftPanelWidth = 300f;
        private const float PortraitHeight = 470f;
        private const string DefaultPortraitPath = "UI/MunoMarriagePortrait";
        private static readonly Texture2D MunoLogo = ContentFinder<Texture2D>.Get("UI/MunoLogo", true);

        private readonly Pawn negotiator;
        private CommPage currentPage;
        private string replyText;
        private string currentPortraitTexPath;
        private float pageOpenTime;
        private bool logisticsCuisineReplied;
        private MunoLogisticsCuisineState logisticsCuisineState;
        private MunoTypewriterTextState typewriter = new MunoTypewriterTextState();

        /// <summary>
        /// 表示和亲通讯当前所在页面。
        /// </summary>
        private enum CommPage
        {
            Greeting,
            MarriageManager,
            LogisticsCuisine,
            Reply
        }

        /// <summary>
        /// 构建一个绑定通讯发起者的和亲通讯窗口。
        /// </summary>
        public Dialog_MunoMarriageComms(Pawn negotiator)
        {
            this.negotiator = negotiator;
            pageOpenTime = Time.realtimeSinceStartup;
            SetPage(CommPage.Greeting, MunoCommDialogueUtility.RandomGreetingLine());
        }

        /// <summary>
        /// 返回和亲通讯窗口的固定尺寸。
        /// </summary>
        public override Vector2 InitialSize => new Vector2(1024f, 680f);

        /// <summary>
        /// 绘制当前和亲通讯页面。
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            if (currentPage == CommPage.Greeting)
            {
                MunoCommUIStyle.DrawBackground(inRect);
                if (MunoCommUIStyle.DrawTerminalHeader(inRect, "缪诺和亲链路", MunoLogo))
                {
                    Close();
                    return;
                }

                DrawGreetingPage(new Rect(inRect.x, inRect.y + 54f, inRect.width, inRect.height - 54f));
                return;
            }

            MunoCommUIStyle.DrawBackground(inRect);
            if (MunoCommUIStyle.DrawTerminalHeader(inRect, "缪诺和亲链路", MunoLogo))
            {
                Close();
                return;
            }

            Rect animatedRect = MunoCommUIStyle.ApplyEntryAnimation(inRect, pageOpenTime);
            Color oldGuiColor = GUI.color;
            GUI.color = new Color(oldGuiColor.r, oldGuiColor.g, oldGuiColor.b, oldGuiColor.a * MunoCommUIStyle.EntryAlpha(pageOpenTime));

            float leftPanelWidth = MunoCommUIConfigUtility.ColumnWidth(MunoCommUIConfigUtility.MarriagePortrait(), LeftPanelWidth);
            Rect leftRect = new Rect(animatedRect.x + 18f, animatedRect.y + 72f, leftPanelWidth, animatedRect.height - 90f);
            Rect rightRect = new Rect(leftRect.xMax + 18f, leftRect.y, animatedRect.width - leftRect.width - 54f, leftRect.height);

            DrawContactPanel(leftRect, currentPortraitTexPath, currentPage == CommPage.LogisticsCuisine ? "后勤管理员" : "和亲管理员");
            if (currentPage == CommPage.MarriageManager)
            {
                DrawMarriageManagerPage(rightRect);
                GUI.color = oldGuiColor;
                return;
            }

            if (currentPage == CommPage.LogisticsCuisine)
            {
                DrawLogisticsCuisinePage(rightRect);
                GUI.color = oldGuiColor;
                return;
            }

            DrawReplyPage(rightRect);
            GUI.color = oldGuiColor;
        }

        /// <summary>
        /// 绘制左侧联系人立绘和身份文本。
        /// </summary>
        private static void DrawContactPanel(Rect rect, string portraitTexPath, string contactLabel)
        {
            MunoCommPortraitLayout layout = MunoCommUIConfigUtility.WithPortraitPath(MunoCommUIConfigUtility.MarriagePortrait(), portraitTexPath);
            Rect portraitRect = MunoCommUIConfigUtility.PanelRect(rect, layout, 0f, 0f, rect.width, PortraitHeight);
            MunoCommUIConfigUtility.DrawPortraitPanel(portraitRect, layout, DefaultPortraitPath);

            Rect labelRect = new Rect(rect.x, portraitRect.yMax + 14f, rect.width, Text.LineHeightOf(GameFont.Medium) + 10f);
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = MunoCommUIStyle.TextColor;
            Widgets.Label(labelRect, contactLabel);
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }

        /// <summary>
        /// 绘制缪诺招待链路和联系人分支页面。
        /// </summary>
        private void DrawGreetingPage(Rect rect)
        {
            MunoCommMainMenuAction action = MunoCommMainMenuView.Draw(rect, "和亲事项管理人", pageOpenTime, typewriter, currentPortraitTexPath);
            if (action == MunoCommMainMenuAction.OpenExchange)
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                SetPage(CommPage.MarriageManager, MunoCommDialogueUtility.RandomManagerPromptLine());
                return;
            }

            if (action == MunoCommMainMenuAction.ForceRefreshMarriage)
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                MunoMarriageRequestUtility.DebugForceRefreshRequest(MunoMarriageDiplomacyService.Component());
                replyText = null;
                SetPage(CommPage.MarriageManager, MunoCommDialogueUtility.RandomManagerPromptLine());
                return;
            }

            if (action == MunoCommMainMenuAction.OpenLogistics)
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                logisticsCuisineReplied = false;
                logisticsCuisineState = MunoLogisticsCuisineComponent.Current()?.State ?? MunoLogisticsCuisineState.Available;
                SetPage(CommPage.LogisticsCuisine, MunoLogisticsCuisinePageView.DialogueForState(logisticsCuisineState));
                return;
            }

            if (action == MunoCommMainMenuAction.Close)
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                Close();
            }
        }

        //绘制后勤管理员料理试吃页面。
        private void DrawLogisticsCuisinePage(Rect rect)
        {
            MunoLogisticsCuisineComponent component = MunoLogisticsCuisineComponent.Current();
            if (!logisticsCuisineReplied && component != null)
            {
                logisticsCuisineState = component.State;
            }

            MunoLogisticsCuisinePageView.PageAction action = MunoLogisticsCuisinePageView.Draw(rect, typewriter, logisticsCuisineState, logisticsCuisineReplied);
            if (action == MunoLogisticsCuisinePageView.PageAction.None)
            {
                return;
            }

            if (CompleteTextIfNeeded())
            {
                return;
            }

            if (action == MunoLogisticsCuisinePageView.PageAction.RequestCuisine)
            {
                logisticsCuisineReplied = true;
                string replyText = "后勤管理员暂时无法送达分子料理。";
                if (component != null)
                {
                    component.TryRequestCuisine(negotiator, out replyText);
                }

                logisticsCuisineState = component?.State ?? logisticsCuisineState;
                SetPage(CommPage.LogisticsCuisine, replyText);
                return;
            }

            if (action == MunoLogisticsCuisinePageView.PageAction.RefuseCuisine)
            {
                logisticsCuisineReplied = true;
                SetPage(CommPage.LogisticsCuisine, MunoLogisticsCuisineText.RejectReplyText);
                return;
            }

            if (action == MunoLogisticsCuisinePageView.PageAction.FeedbackDelicious)
            {
                ApplyLogisticsFeedback(component, MunoLogisticsCuisineFeedback.Delicious);
                return;
            }

            if (action == MunoLogisticsCuisinePageView.PageAction.FeedbackOrdinary)
            {
                ApplyLogisticsFeedback(component, MunoLogisticsCuisineFeedback.Ordinary);
                return;
            }

            if (action == MunoLogisticsCuisinePageView.PageAction.FeedbackAwful)
            {
                ApplyLogisticsFeedback(component, MunoLogisticsCuisineFeedback.Awful);
                return;
            }

            Close();
        }

        //应用玩家对后勤分子料理的反馈并显示管理员回复。
        private void ApplyLogisticsFeedback(MunoLogisticsCuisineComponent component, MunoLogisticsCuisineFeedback feedback)
        {
            logisticsCuisineReplied = true;
            string replyText = component?.ApplyFeedback(feedback, negotiator) ?? MunoLogisticsCuisineText.AwaitingTasteText;
            logisticsCuisineState = component?.State ?? logisticsCuisineState;
            SetPage(CommPage.LogisticsCuisine, replyText);
        }

        /// <summary>
        /// 绘制和亲候选选择页面。
        /// </summary>
        private void DrawMarriageManagerPage(Rect rect)
        {
            if (!MunoMarriageDiplomacyService.TryGetOrCreateCandidates(out List<MunoMarriageCandidateRecord> candidates, out string failReason))
            {
                if (replyText == null)
                {
                    MunoCommDialogueResult noRequestReply = MunoCommDialogueUtility.RandomNoRequestReplyLine();
                    replyText = string.IsNullOrEmpty(failReason) ? noRequestReply.text : failReason;
                    currentPortraitTexPath = string.IsNullOrEmpty(failReason) ? noRequestReply.portraitTexPath : null;
                    typewriter.SetText(replyText);
                }

                DrawReplyPage(rect, allowBack: true);
                return;
            }

            float footerHeight = 58f;
            float gap = 12f;
            Rect dialogueRect = new Rect(rect.x, rect.y, rect.width, 132f);
            Rect footerRect = new Rect(rect.x, rect.yMax - footerHeight, rect.width, footerHeight);
            Rect cardsRect = new Rect(rect.x, dialogueRect.yMax + gap, rect.width, footerRect.y - dialogueRect.yMax - gap * 2f);

            DrawDialoguePanel(dialogueRect);

            float cardGap = 16f;
            float cardHeight = Mathf.Min(330f, cardsRect.height);
            float cardWidth = Mathf.Min(172f, (cardsRect.width - cardGap * 2f) / 3f);
            float totalCardsWidth = cardWidth * 3f + cardGap * 2f;
            float startX = cardsRect.x + (cardsRect.width - totalCardsWidth) * 0.5f;
            float cardY = cardsRect.y + (cardsRect.height - cardHeight) * 0.5f;
            for (int i = 0; i < candidates.Count && i < 3; i++)
            {
                Rect cardRect = new Rect(startX + i * (cardWidth + cardGap), cardY, cardWidth, cardHeight);
                DrawCandidateCard(cardRect, candidates[i].pawn, i + 1);
            }

            DrawFooterButtons(footerRect, showBack: true, showReject: true);
        }

        /// <summary>
        /// 绘制单名和亲候选卡片，并在点击时接受该候选。
        /// </summary>
        private void DrawCandidateCard(Rect rect, Pawn pawn, int index)
        {
            bool hovered = Mouse.IsOver(rect);
            Rect animatedRect = hovered ? rect.ContractedBy(-2f) : rect;
            MunoCommUIStyle.DrawPanel(animatedRect);
            if (hovered)
            {
                Widgets.DrawBoxSolid(animatedRect, new Color(MunoCommUIStyle.AccentColor.r, MunoCommUIStyle.AccentColor.g, MunoCommUIStyle.AccentColor.b, 0.12f));
            }

            MunoCommUIStyle.DrawBorder(animatedRect, hovered ? MunoCommUIStyle.AccentSoftColor : MunoCommUIStyle.BorderColor, hovered ? 2 : 1);
            Rect inner = animatedRect.ContractedBy(10f);
            float badgeHeight = Mathf.Max(30f, Text.LineHeightOf(GameFont.Medium) + 8f);
            float nameHeight = Mathf.Max(44f, Text.LineHeightOf(GameFont.Small) * 2f + 8f);
            Rect badgeRect = new Rect(inner.x, inner.y, inner.width, badgeHeight);
            Rect nameRect = new Rect(inner.x, inner.yMax - nameHeight, inner.width, nameHeight);
            Rect portraitRect = new Rect(inner.x + 8f, badgeRect.yMax + 8f, inner.width - 16f, nameRect.y - badgeRect.yMax - 16f);

            Widgets.DrawBoxSolid(badgeRect, new Color(MunoCommUIStyle.AccentColor.r, MunoCommUIStyle.AccentColor.g, MunoCommUIStyle.AccentColor.b, 0.18f));
            MunoCommUIStyle.DrawBorder(badgeRect, MunoCommUIStyle.BorderColor);
            Widgets.DrawBoxSolid(portraitRect, MunoCommUIStyle.SoftPanelColor);
            if (pawn != null)
            {
                RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(portraitRect.width, portraitRect.height), Rot4.South, new Vector3(0f, 0f, 0.15f), 1.15f, supersample: true, compensateForUIScale: true, renderHeadgear: true, renderClothes: true);
                GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleToFit, alphaBlend: true);
            }

            DrawLightTitle(badgeRect, index.ToString(), GameFont.Medium);
            DrawLightTitle(nameRect, pawn?.LabelShortCap ?? "未知候选", GameFont.Small);

            if (Widgets.ButtonInvisible(animatedRect))
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                AcceptCandidate(pawn);
            }
        }

        /// <summary>
        /// 尝试接受指定候选并进入回复页面。
        /// </summary>
        private void AcceptCandidate(Pawn pawn)
        {
            if (MunoMarriageDiplomacyService.TryAcceptPrincess(negotiator, pawn, out string failReason))
            {
                MunoCommDialogueResult acceptReply = MunoCommDialogueUtility.RandomAcceptReplyLine();
                replyText = acceptReply.text;
                SetPage(CommPage.Reply, acceptReply);
                return;
            }

            replyText = failReason ?? "本次和亲安排出现问题。";
            SetPage(CommPage.Reply, replyText);
        }

        /// <summary>
        /// 绘制接受、拒绝或无请求后的回复页面。
        /// </summary>
        private void DrawReplyPage(Rect rect)
        {
            DrawReplyPage(rect, allowBack: false);
        }

        /// <summary>
        /// 绘制接受、拒绝或无请求后的回复页面，并按需要提供返回上级入口。
        /// </summary>
        private void DrawReplyPage(Rect rect, bool allowBack)
        {
            float footerHeight = 58f;
            Rect replyRect = new Rect(rect.x, rect.y, rect.width, Mathf.Min(240f, rect.height - footerHeight - 12f));
            Rect footerRect = new Rect(rect.x, rect.yMax - footerHeight, rect.width, footerHeight);
            DrawDialoguePanel(replyRect);
            DrawFooterButtons(footerRect, showBack: allowBack, showReject: false);
        }

        /// <summary>
        /// 绘制底部居中按钮栏。
        /// </summary>
        private void DrawFooterButtons(Rect rect, bool showBack, bool showReject)
        {
            MunoCommUIStyle.DrawPanel(rect);

            int buttonCount = 1 + (showBack ? 1 : 0) + (showReject ? 1 : 0);
            float buttonWidth = 150f;
            float buttonHeight = 38f;
            float gap = 12f;
            float totalWidth = buttonCount * buttonWidth + (buttonCount - 1) * gap;
            float x = rect.x + (rect.width - totalWidth) * 0.5f;
            float y = rect.y + (rect.height - buttonHeight) * 0.5f;

            if (showBack)
            {
                Rect backRect = new Rect(x, y, buttonWidth, buttonHeight);
                x += buttonWidth + gap;
                if (MunoCommUIStyle.DrawButton(backRect, "返回"))
                {
                    if (CompleteTextIfNeeded())
                    {
                        return;
                    }

                    SetPage(CommPage.Greeting, MunoCommDialogueUtility.RandomGreetingLine());
                    replyText = null;
                    logisticsCuisineReplied = false;
                    return;
                }
            }

            if (showReject)
            {
                Rect rejectRect = new Rect(x, y, buttonWidth, buttonHeight);
                x += buttonWidth + gap;
                if (MunoCommUIStyle.DrawButton(rejectRect, "拒绝和亲"))
                {
                    if (CompleteTextIfNeeded())
                    {
                        return;
                    }

                    MunoMarriageDiplomacyService.RejectCurrentRequest();
                    MunoCommDialogueResult rejectReply = MunoCommDialogueUtility.RandomRejectReplyLine();
                    replyText = rejectReply.text;
                    SetPage(CommPage.Reply, rejectReply);
                    return;
                }
            }

            Rect closeRect = new Rect(x, y, buttonWidth, buttonHeight);
            if (MunoCommUIStyle.DrawButton(closeRect, "断开通讯"))
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                Close();
            }
        }

        /// <summary>
        /// 切换通讯页面并重置页面进入动效计时。
        /// </summary>
        private void SetPage(CommPage page, string dialogueText)
        {
            currentPage = page;
            pageOpenTime = Time.realtimeSinceStartup;
            currentPortraitTexPath = null;
            typewriter.SetText(dialogueText);
        }

        /// <summary>
        /// 切换通讯页面并应用文本自带的可选立绘覆盖。
        /// </summary>
        private void SetPage(CommPage page, MunoCommDialogueResult dialogue)
        {
            currentPage = page;
            pageOpenTime = Time.realtimeSinceStartup;
            currentPortraitTexPath = dialogue?.portraitTexPath;
            typewriter.SetText(dialogue?.text ?? string.Empty);
        }

        /// <summary>
        /// 绘制带打字机文本的深色对话框。
        /// </summary>
        private void DrawDialoguePanel(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(18f);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = MunoCommUIStyle.TextColor;
            Widgets.Label(inner, typewriter.VisibleText());
            if (Widgets.ButtonInvisible(inner))
            {
                typewriter.Complete();
            }

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }

        /// <summary>
        /// 在文本尚未完整显示时立即展开全文，并返回是否已拦截本次操作。
        /// </summary>
        private bool CompleteTextIfNeeded()
        {
            if (typewriter.Completed)
            {
                return false;
            }

            typewriter.Complete();
            return true;
        }

        /// <summary>
        /// 在深色卡片中绘制浅色居中文本。
        /// </summary>
        private static void DrawLightTitle(Rect rect, string text, GameFont font)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = font;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = MunoCommUIStyle.TextColor;
            Widgets.Label(rect, text);
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }
    }
}
