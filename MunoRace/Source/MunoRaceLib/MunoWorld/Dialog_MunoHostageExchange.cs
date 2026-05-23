using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责显示缪诺通讯交换终端界面，并发起穿梭机人口接收流程。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Dialog_MunoHostageExchange : MunoWindowBase
    {
        private const float HeaderHeight = 42f;
        private const float LeftPanelWidth = 280f;
        private const float CandidateRowHeight = 68f;
        private const float CandidateRowGap = 8f;
        private static readonly Texture2D PortraitTexture = ContentFinder<Texture2D>.Get("UI/MunoCommPortrait", false) ?? BaseContent.BadTex;
        private static readonly Texture2D MunoLogo = ContentFinder<Texture2D>.Get("UI/MunoLogo", true);

        private readonly Pawn negotiator;
        private readonly Map map;
        private Vector2 candidateScrollPosition;
        private Pawn selectedPawn;

        /// <summary>
        /// 构建一个绑定当前通讯发起者与地图环境的缪诺交换终端窗口。
        /// </summary>
        public Dialog_MunoHostageExchange(Pawn negotiator)
        {
            this.negotiator = negotiator;
            map = negotiator?.Map;
        }

        /// <summary>
        /// 为兼容旧的据点交换入口，允许从远行队场景直接构建同一窗口。
        /// </summary>
        public Dialog_MunoHostageExchange(Settlement settlement, Caravan caravan)
        {
            List<Pawn> pawns = caravan?.PawnsListForReading;
            negotiator = pawns != null && pawns.Count > 0 ? pawns[0] : null;
            map = negotiator?.Map;
        }

        /// <summary>
        /// 返回缪诺通讯交换终端窗口的固定初始尺寸。
        /// </summary>
        public override Vector2 InitialSize => new Vector2(1080f, 720f);

        /// <summary>
        /// 绘制整套缪诺通讯交换终端界面，包括立绘、说明与候选目标列表。
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            MunoCommUIStyle.DrawBackground(inRect);
            DrawHeader(inRect);

            Rect leftRect = new Rect(inRect.x + 14f, inRect.y + 56f, LeftPanelWidth, inRect.height - 70f);
            Rect rightRect = new Rect(leftRect.xMax + 14f, leftRect.y, inRect.width - leftRect.width - 28f, leftRect.height);
            Rect topRightRect = new Rect(rightRect.x, rightRect.y, rightRect.width, 208f);
            Rect bottomRightRect = new Rect(rightRect.x, topRightRect.yMax + 12f, rightRect.width, rightRect.height - topRightRect.height - 12f);

            DrawLeftPanel(leftRect);
            DrawMissionPanel(topRightRect);
            DrawCandidatePanel(bottomRightRect);
        }

        /// <summary>
        /// 绘制窗口顶部标题栏、徽记与关闭按钮。
        /// </summary>
        private void DrawHeader(Rect inRect)
        {
            Rect headerRect = new Rect(inRect.x + 12f, inRect.y + 10f, inRect.width - 24f, HeaderHeight);
            MunoCommUIStyle.DrawPanel(headerRect);

            Rect logoRect = new Rect(headerRect.x + 10f, headerRect.y + 6f, 30f, 30f);
            GUI.DrawTexture(logoRect, MunoLogo);

            Text.Font = GameFont.Medium;
            GUI.color = MunoCommUIStyle.GoldColor;
            Widgets.Label(new Rect(logoRect.xMax + 10f, headerRect.y + 6f, 520f, 30f), "缪诺接收链路");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect closeRect = new Rect(headerRect.xMax - 34f, headerRect.y + 6f, 28f, 28f);
            if (Widgets.ButtonImage(closeRect, MunoCommUIStyle.CloseXSmall))
            {
                Close();
            }
        }

        /// <summary>
        /// 绘制左栏联系人立绘、状态与交换摘要。
        /// </summary>
        private void DrawLeftPanel(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);

            float portraitHeight = 440f;
            Rect portraitRect = new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, portraitHeight);
            MunoCommUIStyle.DrawLightPanel(portraitRect);
            DrawPortrait(portraitRect.ContractedBy(4f));

            Rect infoRect = new Rect(rect.x + 10f, portraitRect.yMax + 10f, rect.width - 20f, rect.height - portraitHeight - 30f);
            MunoCommUIStyle.DrawPanel(infoRect);

            Rect inner = infoRect.ContractedBy(10f);
            Text.Font = GameFont.Small;
            GUI.color = MunoCommUIStyle.AccentSoftColor;
            float titleHeight = Text.LineHeight;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, titleHeight), "通讯状态");
            GUI.color = MunoCommUIStyle.TextColor;
            float infoY = inner.y + titleHeight + 6f;
            string statusDesc = "缪诺联络官已确认信号。请选择一名目标，缪诺接收穿梭机将在殖民地内降落并执行人口接收。";
            float statusHeight = Text.CalcHeight(statusDesc, inner.width);
            Widgets.Label(new Rect(inner.x, infoY, inner.width, statusHeight), statusDesc);

            GUI.color = MunoCommUIStyle.AccentSoftColor;
            float rulesTitleY = infoY + statusHeight + 10f;
            Widgets.Label(new Rect(inner.x, rulesTitleY, inner.width, titleHeight), "当前规则");
            GUI.color = MunoCommUIStyle.TextColor;
            string rulesDesc = "每次仅接收 1 名合法目标。\n可接收对象：殖民者、囚犯、奴隶。\n接收完成后，固定有 1 名缪诺成员加入殖民地。";
            float rulesY = rulesTitleY + titleHeight + 6f;
            float rulesHeight = Text.CalcHeight(rulesDesc, inner.width);
            Widgets.Label(new Rect(inner.x, rulesY, inner.width, rulesHeight), rulesDesc);

            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制右上角任务说明、奖励信息与当前会话状态。
        /// </summary>
        private void DrawMissionPanel(Rect rect)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect inner = rect.ContractedBy(12f);

            Text.Font = GameFont.Small;
            GUI.color = MunoCommUIStyle.MutedDarkTextColor;
            float titleHeight = Text.LineHeight;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, titleHeight), "交换说明");

            GUI.color = MunoCommUIStyle.DarkTextColor;
            string statusText = BuildStatusText();
            string desc = "1. 选择 1 名目标并发起接收请求。\n2. 缪诺穿梭机将在地图内降落。\n3. 将选中的目标送入穿梭机后，穿梭机会自动离场。\n4. 离场成功后，一名缪诺成员将加入殖民地。\n\n" + statusText;
            float descHeight = Text.CalcHeight(desc, inner.width);
            Widgets.Label(new Rect(inner.x, inner.y + titleHeight + 8f, inner.width, Mathf.Min(descHeight, inner.height - titleHeight - 8f)), desc);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制候选目标列表、选中目标卡片与主操作按钮。
        /// </summary>
        private void DrawCandidatePanel(Rect rect)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(12f);

            Text.Font = GameFont.Small;
            GUI.color = MunoCommUIStyle.AccentSoftColor;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 26f), "接收目标列表");
            GUI.color = Color.white;

            List<Pawn> candidates = MunoHostageExchangeService.GetExchangeCandidates(map);
            float listWidth = inner.width * 0.58f;
            Rect listRect = new Rect(inner.x, inner.y + 32f, listWidth, inner.height - 32f);
            Rect detailRect = new Rect(listRect.xMax + 12f, listRect.y, inner.width - listRect.width - 12f, inner.height - 82f);
            Rect actionRect = new Rect(detailRect.x, detailRect.yMax + 10f, detailRect.width, 72f);

            DrawCandidateList(listRect, candidates);
            DrawSelectedPawnDetail(detailRect);
            DrawActionArea(actionRect, candidates.Count > 0);
        }

        /// <summary>
        /// 绘制候选目标滚动列表，并支持点击切换当前选中目标。
        /// </summary>
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

        /// <summary>
        /// 绘制单个候选目标卡片，并在点击时切换当前选中项。
        /// </summary>
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

        /// <summary>
        /// 绘制当前选中目标的详细状态卡片，便于在发起前二次确认。
        /// </summary>
        private void DrawSelectedPawnDetail(Rect rect)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect inner = rect.ContractedBy(10f);

            Text.Font = GameFont.Small;
            GUI.color = MunoCommUIStyle.MutedDarkTextColor;
            float lineHeight = Text.LineHeight;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, lineHeight), "选中目标");

            if (selectedPawn == null)
            {
                GUI.color = MunoCommUIStyle.DarkTextColor;
                Widgets.Label(new Rect(inner.x, inner.y + lineHeight + 8f, inner.width, lineHeight), "尚未选中任何目标。");
                GUI.color = Color.white;
                return;
            }

            GUI.color = MunoCommUIStyle.DarkTextColor;
            float y = inner.y + lineHeight + 8f;
            Widgets.Label(new Rect(inner.x, y, inner.width, lineHeight), selectedPawn.LabelCap);
            y += lineHeight + 6f;
            Widgets.Label(new Rect(inner.x, y, inner.width, lineHeight), "身份: " + MunoHostageExchangeService.GetPawnRoleLabel(selectedPawn));
            y += lineHeight + 4f;
            Widgets.Label(new Rect(inner.x, y, inner.width, lineHeight), "状态: " + MunoHostageExchangeService.GetPawnStatusLabel(selectedPawn));
            y += lineHeight + 4f;
            Widgets.Label(new Rect(inner.x, y, inner.width, lineHeight), "年龄: " + selectedPawn.ageTracker.AgeBiologicalYears);
            y += lineHeight + 10f;
            string detailText = "该目标会被缪诺接收穿梭机作为本次唯一合法装载对象。穿梭机离场前，如目标死亡、失效或流程被打断，本次接收将直接失败。";
            float detailHeight = Text.CalcHeight(detailText, inner.width);
            Widgets.Label(new Rect(inner.x, y, inner.width, Mathf.Min(detailHeight, inner.yMax - y)), detailText);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 绘制主操作区，并在条件满足时发起缪诺穿梭机接收流程。
        /// </summary>
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

            Rect buttonRect = new Rect(inner.x, inner.y, inner.width, 34f);
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
            float footerHeight = Text.CalcHeight(footerText, inner.width);
            Widgets.Label(new Rect(inner.x, inner.y + 40f, inner.width, footerHeight), footerText);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 按通讯终端立绘卡片比例裁切并绘制联系人图像，使人物重心更贴合画面。
        /// </summary>
        private static void DrawPortrait(Rect rect)
        {
            GUI.BeginGroup(rect);
            Widgets.DrawBoxSolid(new Rect(0f, 0f, rect.width, rect.height), new Color(0.77f, 0.80f, 0.80f));

            float scale = Mathf.Max(rect.width / PortraitTexture.width, rect.height / PortraitTexture.height) * 0.92f;
            float drawWidth = PortraitTexture.width * scale;
            float drawHeight = PortraitTexture.height * scale;
            float drawX = (rect.width - drawWidth) * 0.5f + 8f;
            float drawY = (rect.height - drawHeight) * 0.18f;

            GUI.DrawTexture(new Rect(drawX, drawY, drawWidth, drawHeight), PortraitTexture, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
        }

        /// <summary>
        /// 组合当前会话状态的说明文本，用于任务面板即时反馈进度。
        /// </summary>
        private static string BuildStatusText()
        {
            MunoShuttleExchangeSession session = MunoShuttleExchangeService.CurrentSession();
            if (!session.HasActiveSession)
            {
                return "当前没有进行中的缪诺接收流程。";
            }

            if (session.SelectedPawn != null)
            {
                return "进行中：缪诺接收穿梭机正在等待目标 “" + session.SelectedPawn.LabelShort + "” 登机。";
            }

            return "进行中：缪诺接收穿梭机正在等待目标登机。";
        }
    }
}
