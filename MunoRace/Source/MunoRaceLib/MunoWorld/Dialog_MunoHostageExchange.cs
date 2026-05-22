using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 显示当前远行队可上交成员列表，并执行缪诺据点单人交换操作。
    /// </summary>
    public class Dialog_MunoHostageExchange : Window
    {
        private const float RowHeight = 36f;
        private const float RowSpacing = 6f;
        private readonly Settlement settlement;
        private readonly Caravan caravan;
        private Vector2 scrollPosition;

        /// <summary>
        /// 构建一个绑定当前缪诺据点与远行队的交换窗口。
        /// </summary>
        public Dialog_MunoHostageExchange(Settlement settlement, Caravan caravan)
        {
            this.settlement = settlement;
            this.caravan = caravan;
            optionalTitle = "缪诺成员交换";
            doCloseX = true;
            doCloseButton = true;
            draggable = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        /// <summary>
        /// 返回交换窗口的初始尺寸。
        /// </summary>
        public override Vector2 InitialSize => new Vector2(760f, 560f);

        /// <summary>
        /// 绘制交换说明、候选列表和空列表提示。
        /// </summary>
        public override void DoWindowContents(Rect inRect)
        {
            List<Pawn> candidates = MunoHostageExchangeService.GetExchangeCandidates(caravan);
            string introText = "上交当前远行队中的一名殖民者、囚犯或奴隶，即可立即换取一名缪诺成员加入当前远行队。\n\n每次只能交换 1 人，交换成功后对方会脱离玩家阵营并转入当前缪诺据点。";

            Text.Font = GameFont.Small;
            float introHeight = Text.CalcHeight(introText, inRect.width) + 6f;
            Rect introRect = new Rect(inRect.x, inRect.y, inRect.width, introHeight);
            Widgets.Label(introRect, introText);

            float tableTop = introRect.yMax + 8f;
            Rect headerRect = new Rect(inRect.x, tableTop, inRect.width, Text.LineHeight + 6f);
            DrawHeader(headerRect);

            float bodyTop = headerRect.yMax + 4f;
            float bottomPadding = 10f;
            if (doCloseButton)
            {
                bottomPadding += FooterRowHeight;
            }

            float availableBodyHeight = inRect.yMax - bodyTop - bottomPadding;
            Rect bodyRect = new Rect(inRect.x, bodyTop, inRect.width, availableBodyHeight);
            if (bodyRect.height < 120f)
            {
                bodyRect.height = 120f;
            }

            if (candidates.Count == 0)
            {
                Widgets.DrawMenuSection(bodyRect);
                Rect emptyRect = bodyRect.ContractedBy(12f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(emptyRect, "当前远行队没有符合条件的可交换目标。");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            DrawCandidateList(bodyRect, candidates);
        }

        /// <summary>
        /// 绘制列表表头，说明每列展示的信息。
        /// </summary>
        private void DrawHeader(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(6f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, 180f, innerRect.height), "姓名");
            Widgets.Label(new Rect(innerRect.x + 188f, innerRect.y, 90f, innerRect.height), "身份");
            Widgets.Label(new Rect(innerRect.x + 286f, innerRect.y, 70f, innerRect.height), "年龄");
            Widgets.Label(new Rect(innerRect.x + 364f, innerRect.y, 180f, innerRect.height), "状态");
            Widgets.Label(new Rect(innerRect.x + 552f, innerRect.y, 120f, innerRect.height), "操作");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 绘制滚动候选列表，并为每名候选人提供单独交换按钮。
        /// </summary>
        private void DrawCandidateList(Rect rect, List<Pawn> candidates)
        {
            Widgets.DrawMenuSection(rect);

            float totalViewHeight = candidates.Count * (RowHeight + RowSpacing) + 8f;
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalViewHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float rowTop = 4f;
            for (int i = 0; i < candidates.Count; i++)
            {
                Pawn pawn = candidates[i];
                Rect rowRect = new Rect(4f, rowTop, viewRect.width - 8f, RowHeight);
                DrawCandidateRow(rowRect, pawn);
                rowTop += RowHeight + RowSpacing;
            }
            Widgets.EndScrollView();
        }

        /// <summary>
        /// 绘制单个候选 Pawn 的信息行与交换按钮。
        /// </summary>
        private void DrawCandidateRow(Rect rect, Pawn pawn)
        {
            Widgets.DrawHighlightIfMouseover(rect);
            Rect innerRect = rect.ContractedBy(6f);
            float lineHeight = Mathf.Max(Text.LineHeight, innerRect.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(innerRect.x, innerRect.y, 180f, lineHeight), pawn.LabelCap);
            Widgets.Label(new Rect(innerRect.x + 188f, innerRect.y, 90f, lineHeight), MunoHostageExchangeService.GetPawnRoleLabel(pawn));
            Widgets.Label(new Rect(innerRect.x + 286f, innerRect.y, 70f, lineHeight), pawn.ageTracker.AgeBiologicalYears.ToString());
            Widgets.Label(new Rect(innerRect.x + 364f, innerRect.y, 180f, lineHeight), MunoHostageExchangeService.GetPawnStatusLabel(pawn));

            Rect buttonRect = new Rect(innerRect.x + 552f, innerRect.y + 2f, 96f, 32f);
            if (Widgets.ButtonText(buttonRect, "交换"))
            {
                TryExecuteExchange(pawn);
            }

            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 尝试执行对指定 Pawn 的交换，并向玩家反馈结果。
        /// </summary>
        private void TryExecuteExchange(Pawn pawn)
        {
            if (MunoHostageExchangeService.TryExchangePawn(settlement, caravan, pawn, out string failReason, out Pawn joinedPawn))
            {
                Messages.Message("交换完成，新的缪诺成员已加入当前远行队。", joinedPawn, MessageTypeDefOf.PositiveEvent);
                return;
            }

            Messages.Message(failReason ?? "交换失败。", MessageTypeDefOf.RejectInput, false);
        }
    }
}
