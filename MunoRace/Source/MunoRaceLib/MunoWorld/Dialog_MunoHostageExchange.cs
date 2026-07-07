using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace MunoRaceLib.MunoWorld
{
    //负责显示缪诺通讯交换终端界面，并发起穿梭机人口接收流程。

    [StaticConstructorOnStartup]
    public class Dialog_MunoHostageExchange : MunoWindowBase
    {
        private const float LeftPanelWidth = 280f;
        private const float CandidateRowHeight = 68f;
        private const float CandidateRowGap = 8f;
        private static readonly Texture2D PortraitTexture = ContentFinder<Texture2D>.Get("UI/MunoCommPortrait", false) ?? BaseContent.BadTex;
        private static readonly Texture2D MunoLogo = ContentFinder<Texture2D>.Get("UI/MunoLogo", true);
        private static float portraitOffsetX = -16.19f;
        private static float portraitOffsetY = 0.400f;
        private static float portraitScale = 1.033f;

        private readonly Pawn negotiator;
        private readonly Map map;
        private Vector2 candidateScrollPosition;
        private Vector2 leftInfoScrollPosition;
        private Vector2 missionScrollPosition;
        private Vector2 selectedDetailScrollPosition;
        private Pawn selectedPawn;
        private CommPage currentPage;
        private float pageOpenTime;
        private MunoTypewriterTextState typewriter = new MunoTypewriterTextState();
        //表示缪诺通讯窗口当前显示的页面。

        private enum CommPage
        {
            MainMenu,
            Exchange
        }
        //构建一个绑定当前通讯发起者与地图环境的缪诺交换终端窗口。

        public Dialog_MunoHostageExchange(Pawn negotiator)
        {
            this.negotiator = negotiator;
            map = negotiator?.Map;
            pageOpenTime = Time.realtimeSinceStartup;
            typewriter.SetText(MunoCommDialogueUtility.RandomGreeting());
        }
        //构建人口接收窗口，并按需要直接进入接收流程页。
        public Dialog_MunoHostageExchange(Pawn negotiator, bool startAtExchange) : this(negotiator)
        {
            if (startAtExchange)
            {
                SetPage(CommPage.Exchange);
            }
        }
        //为兼容旧的据点交换入口，允许从远行队场景直接构建同一窗口。

        public Dialog_MunoHostageExchange(Settlement settlement, Caravan caravan)
        {
            List<Pawn> pawns = caravan?.PawnsListForReading;
            negotiator = pawns != null && pawns.Count > 0 ? pawns[0] : null;
            map = negotiator?.Map;
            pageOpenTime = Time.realtimeSinceStartup;
            typewriter.SetText(MunoCommDialogueUtility.RandomGreeting());
        }
        //返回缪诺通讯交换终端窗口的固定初始尺寸。

        public override Vector2 InitialSize => new Vector2(1024f, 680f);
        //按当前页面绘制缪诺通讯终端界面。

        public override void DoWindowContents(Rect inRect)
        {
            if (currentPage == CommPage.MainMenu)
            {
                MunoCommUIStyle.DrawBackground(inRect);
                if (MunoCommUIStyle.DrawTerminalHeader(inRect, "缪诺接收链路", MunoLogo))
                {
                    Close();
                    return;
                }

                DrawMainMenuPage(new Rect(inRect.x, inRect.y + 54f, inRect.width, inRect.height - 54f));
                return;
            }

            MunoCommUIStyle.DrawBackground(inRect);
            DrawExchangePage(inRect);
        }
        //绘制首页入口布局，包括联络员立绘、缪诺招待链路、可选管理员入口与断开通讯按钮。

        private void DrawMainMenuPage(Rect inRect)
        {
            MunoCommMainMenuAction action = MunoCommMainMenuView.Draw(inRect, pageOpenTime, typewriter);
            if (action == MunoCommMainMenuAction.OpenExchange)
            {
                if (CompleteTextIfNeeded())
                {
                    return;
                }

                SetPage(CommPage.Exchange);
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
        //在文本尚未完整显示时立即展开全文，并返回是否已拦截本次操作。

        private bool CompleteTextIfNeeded()
        {
            if (typewriter.Completed)
            {
                return false;
            }

            typewriter.Complete();
            return true;
        }
        //绘制原有交换流程页面，包括标题栏、立绘、说明与候选目标列表。

        private void DrawExchangePage(Rect inRect)
        {
            if (MunoCommUIStyle.DrawTerminalHeader(inRect, "缪诺接收链路", MunoLogo))
            {
                Close();
                return;
            }

            Rect animatedRect = MunoCommUIStyle.ApplyEntryAnimation(inRect, pageOpenTime);
            Color oldGuiColor = GUI.color;
            GUI.color = new Color(oldGuiColor.r, oldGuiColor.g, oldGuiColor.b, oldGuiColor.a * MunoCommUIStyle.EntryAlpha(pageOpenTime));

            Rect leftRect = new Rect(animatedRect.x + 14f, animatedRect.y + 56f, LeftPanelWidth, animatedRect.height - 70f);
            Rect rightRect = new Rect(leftRect.xMax + 14f, leftRect.y, inRect.width - leftRect.width - 28f, leftRect.height);
            Rect topRightRect = new Rect(rightRect.x, rightRect.y, rightRect.width, 228f);
            Rect bottomRightRect = new Rect(rightRect.x, topRightRect.yMax + 12f, rightRect.width, rightRect.height - topRightRect.height - 12f);

            DrawLeftPanel(leftRect);
            DrawMissionPanel(topRightRect);
            DrawCandidatePanel(bottomRightRect);
            GUI.color = oldGuiColor;
        }
        //切换通讯页面并重置页面进入动效计时。

        private void SetPage(CommPage page)
        {
            currentPage = page;
            pageOpenTime = Time.realtimeSinceStartup;
        }
        //绘制左栏联系人立绘、状态与交换摘要。

        private void DrawLeftPanel(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);

            float portraitHeight = 404f;
            Rect portraitRect = new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, portraitHeight);
            MunoCommUIStyle.DrawLightPanel(portraitRect);
            DrawPortrait(portraitRect.ContractedBy(4f));

            Rect infoRect = new Rect(rect.x + 10f, portraitRect.yMax + 10f, rect.width - 20f, rect.height - portraitHeight - 30f);
            MunoCommUIStyle.DrawPanel(infoRect);

            Rect inner = infoRect.ContractedBy(10f);
            Rect viewRect = new Rect(0f, 0f, inner.width - 16f, CalculateLeftInfoContentHeight(inner.width - 16f));
            Widgets.BeginScrollView(inner, ref leftInfoScrollPosition, viewRect);

            Text.Font = GameFont.Small;
            GUI.color = MunoCommUIStyle.AccentSoftColor;
            float titleHeight = Text.LineHeight;
            Widgets.Label(new Rect(0f, 0f, viewRect.width, titleHeight), "通讯状态");
            GUI.color = MunoCommUIStyle.TextColor;
            float infoY = titleHeight + 6f;
            string statusDesc = "缪诺联络官已确认信号。军事管理员会派遣接收穿梭机，在殖民地内接收本次选中的目标。";
            float statusHeight = Text.CalcHeight(statusDesc, viewRect.width);
            Widgets.Label(new Rect(0f, infoY, viewRect.width, statusHeight), statusDesc);

            GUI.color = MunoCommUIStyle.AccentSoftColor;
            float rulesTitleY = infoY + statusHeight + 10f;
            Widgets.Label(new Rect(0f, rulesTitleY, viewRect.width, titleHeight), "当前规则");
            GUI.color = MunoCommUIStyle.TextColor;
            string rulesDesc = "每次仅接收 1 名选中目标。\n未选中的殖民者、囚犯、奴隶都不能进入穿梭机。\n接收完成后，固定有 1 名缪诺成员加入殖民地。";
            float rulesY = rulesTitleY + titleHeight + 6f;
            float rulesHeight = Text.CalcHeight(rulesDesc, viewRect.width);
            Widgets.Label(new Rect(0f, rulesY, viewRect.width, rulesHeight), rulesDesc);

            Widgets.EndScrollView();
            GUI.color = Color.white;
        }
        //绘制右上角任务说明、奖励信息与当前会话状态。

        private void DrawMissionPanel(Rect rect)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect inner = rect.ContractedBy(12f);
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = MunoCommUIStyle.MutedDarkTextColor;
            float titleHeight = Text.LineHeightOf(GameFont.Small) + 2f;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, titleHeight), "交换说明");

            GUI.color = MunoCommUIStyle.DarkTextColor;
            string statusText = BuildStatusText();
            string desc = "1. 选择目标并发起接收请求。\n2. 穿梭机将在殖民地内降落。\n3. 只有本次选中的目标可以进入穿梭机。\n4. 目标装入后穿梭机会自动离场。\n5. 离场成功后，一名缪诺成员将加入殖民地。\n\n" + statusText;
            Rect outRect = new Rect(inner.x, inner.y + titleHeight + 8f, inner.width, inner.height - titleHeight - 8f);
            float viewWidth = outRect.width - 16f;
            float descHeight = Mathf.Ceil(Text.CalcHeight(desc, viewWidth)) + 4f;
            Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(outRect.height, descHeight));
            Widgets.BeginScrollView(outRect, ref missionScrollPosition, viewRect);
            Widgets.Label(new Rect(0f, 0f, viewRect.width, descHeight), desc);
            Widgets.EndScrollView();
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }
        //绘制候选目标列表、选中目标卡片与主操作按钮。

        private void DrawCandidatePanel(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(12f);

            Text.Font = GameFont.Small;
            GUI.color = MunoCommUIStyle.AccentSoftColor;
            float titleHeight = Text.LineHeightOf(GameFont.Small) + 4f;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, titleHeight), "接收目标列表");
            GUI.color = Color.white;

            List<Pawn> candidates = MunoHostageExchangeService.GetExchangeCandidates(map);
            float listWidth = inner.width * 0.58f;
            float actionHeight = Mathf.Max(118f, Text.LineHeightOf(GameFont.Small) * 3f + 58f);
            float actionGap = 10f;
            Rect listRect = new Rect(inner.x, inner.y + titleHeight + 8f, listWidth, inner.height - titleHeight - 8f);
            Rect detailRect = new Rect(listRect.xMax + 12f, listRect.y, inner.width - listRect.width - 12f, listRect.height - actionHeight - actionGap);
            Rect actionRect = new Rect(detailRect.x, detailRect.yMax + actionGap, detailRect.width, actionHeight);

            DrawCandidateList(listRect, candidates);
            DrawSelectedPawnDetail(detailRect);
            DrawActionArea(actionRect, candidates.Count > 0);
        }
        //绘制候选目标滚动列表，并支持点击切换当前选中目标。

        private void DrawCandidateList(Rect rect, List<Pawn> candidates)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect bodyRect = rect.ContractedBy(8f);

            if (candidates.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = MunoCommUIStyle.DarkTextColor;
                Widgets.Label(bodyRect, "当前地图没有可供缪诺接收的合法目标。");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            if (selectedPawn == null || !candidates.Contains(selectedPawn))
            {
                selectedPawn = candidates[0];
            }

            float viewHeight = candidates.Count * (CandidateRowHeight + CandidateRowGap) + 4f;
            Rect viewRect = new Rect(0f, 0f, bodyRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(bodyRect, ref candidateScrollPosition, viewRect);

            float y = 2f;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn pawn = candidates[i];
                Rect rowRect = new Rect(0f, y, viewRect.width, CandidateRowHeight);
                DrawCandidateRow(rowRect, pawn, pawn == selectedPawn);
                y += CandidateRowHeight + CandidateRowGap;
            }

            Widgets.EndScrollView();
        }
        //绘制单个候选目标卡片，并在点击时切换当前选中项。

        private void DrawCandidateRow(Rect rect, Pawn pawn, bool selected)
        {
            Widgets.DrawBoxSolid(rect, selected ? new Color(0.26f, 0.80f, 0.74f, 0.16f) : new Color(0f, 0f, 0f, 0.08f));
            MunoCommUIStyle.DrawBorder(rect, selected ? MunoCommUIStyle.AccentColor : new Color(0.32f, 0.45f, 0.45f));

            if (Widgets.ButtonInvisible(rect))
            {
                selectedPawn = pawn;
            }

            Rect inner = rect.ContractedBy(8f);
            Widgets.ThingIcon(new Rect(inner.x, inner.y + 6f, 42f, 42f), pawn);

            float textX = inner.x + 52f;
            float textWidth = inner.width - 52f;
            float topHeight = Text.LineHeight;
            GUI.color = MunoCommUIStyle.DarkTextColor;
            Widgets.Label(new Rect(textX, inner.y + 2f, textWidth, topHeight), pawn.LabelCap);

            string metaText = MunoHostageExchangeService.GetPawnRoleLabel(pawn)
                + "    年龄 " + pawn.ageTracker.AgeBiologicalYears
                + "    " + MunoHostageExchangeService.GetPawnStatusLabel(pawn);
            GUI.color = MunoCommUIStyle.MutedDarkTextColor;
            Widgets.Label(new Rect(textX, inner.y + topHeight + 6f, textWidth, topHeight), metaText);
            GUI.color = Color.white;
        }
        //绘制当前选中目标的详细状态卡片，便于在发起前二次确认。

        private void DrawSelectedPawnDetail(Rect rect)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect inner = rect.ContractedBy(10f);
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = MunoCommUIStyle.MutedDarkTextColor;
            float lineHeight = Text.LineHeightOf(GameFont.Small) + 2f;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, lineHeight), "选中目标");

            if (selectedPawn == null)
            {
                GUI.color = MunoCommUIStyle.DarkTextColor;
                Widgets.Label(new Rect(inner.x, inner.y + lineHeight + 8f, inner.width, lineHeight), "尚未选中任何目标。");
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
                return;
            }

            GUI.color = MunoCommUIStyle.DarkTextColor;
            Rect outRect = new Rect(inner.x, inner.y + lineHeight + 8f, inner.width, inner.height - lineHeight - 8f);
            float viewWidth = outRect.width - 16f;
            string detailText = selectedPawn.LabelCap
                + "\n身份: " + MunoHostageExchangeService.GetPawnRoleLabel(selectedPawn)
                + "\n状态: " + MunoHostageExchangeService.GetPawnStatusLabel(selectedPawn)
                + "\n年龄: " + selectedPawn.ageTracker.AgeBiologicalYears
                + "\n\n该目标会作为本次唯一接收对象。穿梭机抵达后，未选中的殖民者、囚犯或奴隶都不能进入。";
            float detailHeight = Mathf.Ceil(Text.CalcHeight(detailText, viewWidth)) + 4f;
            Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(outRect.height, detailHeight));
            Widgets.BeginScrollView(outRect, ref selectedDetailScrollPosition, viewRect);
            Widgets.Label(new Rect(0f, 0f, viewRect.width, detailHeight), detailText);
            Widgets.EndScrollView();
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }
        //绘制主操作区，并在条件满足时发起缪诺穿梭机接收流程。

        private void DrawActionArea(Rect rect, bool hasCandidates)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(10f);

            string disabledReason = null;
            if (map == null || negotiator == null)
            {
                disabledReason = "当前谈判者不在有效地图中。";
            }
            else if (!hasCandidates)
            {
                disabledReason = "当前地图没有合法目标。";
            }
            else if (selectedPawn == null)
            {
                disabledReason = "请先选择一个接收目标。";
            }
            else if (MunoShuttleExchangeService.CurrentSession().HasActiveSession)
            {
                disabledReason = "已有缪诺接收穿梭机正在执行任务。";
            }

            float buttonHeight = Mathf.Max(38f, Text.LineHeightOf(GameFont.Small) + 10f);
            Rect buttonRect = new Rect(inner.x, inner.y, inner.width, buttonHeight);
            bool active = disabledReason == null;
            if (MunoCommUIStyle.DrawButton(buttonRect, "请求缪诺接收穿梭机", active))
            {
                if (MunoShuttleExchangeService.TryStartExchange(negotiator, selectedPawn, out string failReason))
                {
                    Messages.Message("缪诺接收穿梭机已在殖民地内进场，请将选中目标送入穿梭机。", MessageTypeDefOf.TaskCompletion, false);
                    Close();
                }
                else
                {
                    Messages.Message(failReason ?? "未能发起缪诺接收流程。", MessageTypeDefOf.RejectInput, false);
                }
            }

            GUI.color = MunoCommUIStyle.SubtleTextColor;
            string footerText = active ? "奖励固定为 1 名缪诺殖民者。" : disabledReason;
            float footerHeight = Mathf.Ceil(Text.CalcHeight(footerText, inner.width)) + 2f;
            Rect footerRect = new Rect(inner.x, inner.y + buttonHeight + 8f, inner.width, footerHeight);
            Widgets.Label(footerRect, footerText);
            GUI.color = Color.white;
        }
        //按通讯终端立绘卡片比例裁切并绘制联系人图像，使人物重心更贴合画面。

        private static void DrawPortrait(Rect rect)
        {
            GUI.BeginGroup(rect);
            Widgets.DrawBoxSolid(new Rect(0f, 0f, rect.width, rect.height), new Color(0.14f, 0.19f, 0.19f));

            float scale = Mathf.Max(rect.width / PortraitTexture.width, rect.height / PortraitTexture.height) * portraitScale;
            float drawWidth = PortraitTexture.width * scale;
            float drawHeight = PortraitTexture.height * scale;
            float drawX = (rect.width - drawWidth) * 0.5f + portraitOffsetX;
            float drawY = (rect.height - drawHeight) * portraitOffsetY;

            GUI.DrawTexture(new Rect(drawX, drawY, drawWidth, drawHeight), PortraitTexture, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
        }
        //计算左下信息区完整正文所需高度，供滚动面板安全容纳所有中文文案。

        private static float CalculateLeftInfoContentHeight(float width)
        {
            Text.Font = GameFont.Small;
            float lineHeight = Text.LineHeight;
            string statusDesc = "缪诺联络官已确认信号。军事管理员会派遣接收穿梭机，在殖民地内接收本次选中的目标。";
            string rulesDesc = "每次仅接收 1 名选中目标。\n未选中的殖民者、囚犯、奴隶都不能进入穿梭机。\n接收完成后，固定有 1 名缪诺成员加入殖民地。";
            float statusHeight = Text.CalcHeight(statusDesc, width);
            float rulesHeight = Text.CalcHeight(rulesDesc, width);
            return lineHeight + 6f + statusHeight + 10f + lineHeight + 6f + rulesHeight + 4f;
        }
        //组合当前会话状态的说明文本，用于任务面板即时反馈进度。

        private static string BuildStatusText()
        {
            MunoShuttleExchangeSession session = MunoShuttleExchangeService.CurrentSession();
            if (!session.HasActiveSession)
            {
                return "当前没有进行中的缪诺接收流程。";
            }

            if (session.TargetLoaded && session.SelectedPawn != null)
            {
                return "进行中：缪诺接收穿梭机已接收目标 “" + session.SelectedPawn.LabelShort + "”，正在离场。";
            }

            if (session.SelectedPawn != null)
            {
                return "进行中：缪诺接收穿梭机正在等待 “" + session.SelectedPawn.LabelShort + "” 登机。";
            }

            return "进行中：缪诺接收穿梭机正在等待选中目标登机。";
        }
    }
}
