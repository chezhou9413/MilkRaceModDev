using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //表示多人交换面板在当前帧触发的页面或提交动作。
    public enum MunoHostageExchangePanelAction
    {
        None,
        OpenRewards,
        BackToTargets,
        Submit
    }

    //负责绘制多人候选选择页、奖励分配页和锁定物资预览。
    public static class MunoHostageExchangePanelView
    {
        private const float RowGap = 6f;
        private const float FooterHeight = 52f;
        private const float PawnIconSize = 44f;

        //绘制人员选择页并返回本帧操作结果。
        public static MunoHostageExchangePanelAction DrawTargetPage(Rect rect, List<Pawn> candidates, MunoHostageExchangeDraft draft, ref Vector2 listScroll, bool caravanMode)
        {
            draft.SyncCandidates(candidates);
            Rect bodyRect = new Rect(rect.x, rect.y, rect.width, rect.height - FooterHeight - 8f);
            Rect footerRect = new Rect(rect.x, bodyRect.yMax + 8f, rect.width, FooterHeight);
            float leftWidth = bodyRect.width * 0.66f;
            Rect listRect = new Rect(bodyRect.x, bodyRect.y, leftWidth, bodyRect.height);
            Rect summaryRect = new Rect(listRect.xMax + 10f, bodyRect.y, bodyRect.width - leftWidth - 10f, bodyRect.height);

            DrawCandidateList(listRect, candidates, draft, ref listScroll);
            DrawSelectionSummary(summaryRect, candidates, draft, caravanMode);

            bool active = draft.SelectedCount > 0;
            string label = "进入奖励设置（" + draft.SelectedCount + " 人）";
            Rect nextRect = new Rect(footerRect.xMax - 260f, footerRect.y + 6f, 260f, 40f);
            if (MunoCommUIStyle.DrawButton(nextRect, label, active))
            {
                return MunoHostageExchangePanelAction.OpenRewards;
            }

            if (!active)
            {
                TooltipHandler.TipRegion(nextRect, candidates.Count == 0 ? "当前没有符合条件的交换目标。" : "至少选择一名交换目标。\n请先勾选殖民者、囚犯或奴隶。");
            }

            return MunoHostageExchangePanelAction.None;
        }

        //绘制奖励分配页并返回本帧操作结果。
        public static MunoHostageExchangePanelAction DrawRewardPage(Rect rect, MunoHostageExchangeDraft draft, ref Vector2 rewardScroll, ref Vector2 previewScroll, bool caravanMode, bool exchangeBlocked, string blockedReason)
        {
            draft.EnsureItemPreview(out string previewError);
            Rect bodyRect = new Rect(rect.x, rect.y, rect.width, rect.height - FooterHeight - 8f);
            Rect footerRect = new Rect(rect.x, bodyRect.yMax + 8f, rect.width, FooterHeight);
            float leftWidth = bodyRect.width * 0.58f;
            Rect assignmentRect = new Rect(bodyRect.x, bodyRect.y, leftWidth, bodyRect.height);
            Rect previewRect = new Rect(assignmentRect.xMax + 10f, bodyRect.y, bodyRect.width - leftWidth - 10f, bodyRect.height);

            DrawRewardAssignments(assignmentRect, draft, ref rewardScroll);
            DrawRewardPreview(previewRect, draft, ref previewScroll, previewError, caravanMode);

            if (MunoCommUIStyle.DrawButton(new Rect(footerRect.x, footerRect.y + 6f, 190f, 40f), "返回人员选择"))
            {
                return MunoHostageExchangePanelAction.BackToTargets;
            }

            bool previewValid = previewError.NullOrEmpty();
            bool submitActive = draft.SelectedCount > 0 && previewValid && !exchangeBlocked;
            string submitLabel = caravanMode
                ? "确认批量交换（" + draft.SelectedCount + " 人）"
                : "请求接收穿梭机（" + draft.SelectedCount + " 人）";
            Rect submitRect = new Rect(footerRect.xMax - 280f, footerRect.y + 6f, 280f, 40f);
            if (MunoCommUIStyle.DrawButton(submitRect, submitLabel, submitActive))
            {
                return MunoHostageExchangePanelAction.Submit;
            }

            if (!submitActive)
            {
                string reason = !previewValid
                    ? previewError
                    : exchangeBlocked
                        ? blockedReason
                        : draft.SelectedCount == 0
                            ? "至少选择一名交换目标。"
                            : "当前交换暂时不可用。";
                if (!reason.NullOrEmpty())
                {
                    TooltipHandler.TipRegion(submitRect, reason);
                }
            }

            return MunoHostageExchangePanelAction.None;
        }

        //绘制带全选和清空命令的候选人员列表。
        private static void DrawCandidateList(Rect rect, List<Pawn> candidates, MunoHostageExchangeDraft draft, ref Vector2 scrollPosition)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(10f);
            float lineHeight = Text.LineHeightOf(GameFont.Small) + 4f;
            Rect titleRect = new Rect(inner.x, inner.y, inner.width - 190f, lineHeight);
            DrawOneLineLabel(titleRect, "接收目标  已选 " + draft.SelectedCount + " / " + candidates.Count, MunoCommUIStyle.AccentSoftColor);

            float commandWidth = 82f;
            Rect selectAllRect = new Rect(inner.xMax - commandWidth * 2f - 8f, inner.y, commandWidth, lineHeight + 4f);
            Rect clearRect = new Rect(selectAllRect.xMax + 8f, inner.y, commandWidth, lineHeight + 4f);
            if (MunoCommUIStyle.DrawButton(selectAllRect, "全选", candidates.Count > 0))
            {
                draft.SelectAll(candidates);
            }
            if (MunoCommUIStyle.DrawButton(clearRect, "清空", draft.SelectedCount > 0))
            {
                draft.ClearSelection();
            }

            Rect outRect = new Rect(inner.x, inner.y + lineHeight + 12f, inner.width, inner.height - lineHeight - 12f);
            if (candidates.Count == 0)
            {
                DrawCenteredLabel(outRect, "当前没有符合条件的殖民者、囚犯或奴隶。");
                return;
            }

            float viewWidth = outRect.width - 16f;
            float rowHeight = GetPawnRowHeight();
            float viewHeight = candidates.Count * (rowHeight + RowGap) + 2f;
            Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(outRect.height, viewHeight));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            try
            {
                float y = 0f;
                for (int i = 0; i < candidates.Count; i++)
                {
                    Pawn pawn = candidates[i];
                    DrawCandidateRow(new Rect(0f, y, viewWidth, rowHeight), pawn, draft);
                    y += rowHeight + RowGap;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        //绘制单名候选的复选框、头像和状态信息。
        private static void DrawCandidateRow(Rect rect, Pawn pawn, MunoHostageExchangeDraft draft)
        {
            bool selected = draft.IsSelected(pawn);
            Widgets.DrawBoxSolid(rect, selected ? new Color(0.26f, 0.80f, 0.74f, 0.16f) : new Color(0f, 0f, 0f, 0.08f));
            MunoCommUIStyle.DrawBorder(rect, selected ? MunoCommUIStyle.AccentColor : new Color(0.32f, 0.45f, 0.45f));

            Rect inner = rect.ContractedBy(8f);
            bool newSelected = selected;
            Widgets.Checkbox(new Vector2(inner.x, inner.y + (inner.height - 24f) * 0.5f), ref newSelected);
            if (newSelected != selected)
            {
                draft.SetSelected(pawn, newSelected);
            }

            Rect iconRect = new Rect(inner.x + 34f, inner.y + (inner.height - PawnIconSize) * 0.5f, PawnIconSize, PawnIconSize);
            Widgets.ThingIcon(iconRect, pawn);
            float textX = iconRect.xMax + 10f;
            float textWidth = inner.xMax - textX;
            float lineHeight = Text.LineHeightOf(GameFont.Small) + 2f;
            DrawOneLineLabel(new Rect(textX, inner.y + 2f, textWidth, lineHeight), pawn.LabelCap, MunoCommUIStyle.DarkTextColor);
            string meta = MunoHostageExchangeService.GetPawnRoleLabel(pawn)
                + "    年龄 " + pawn.ageTracker.AgeBiologicalYears
                + "    " + MunoHostageExchangeService.GetPawnStatusLabel(pawn)
                + "    价值 " + pawn.MarketValue.ToStringMoney();
            DrawOneLineLabel(new Rect(textX, inner.y + lineHeight + 7f, textWidth, lineHeight), meta, MunoCommUIStyle.MutedDarkTextColor);
        }

        //绘制当前人员选择、规则和进行中会话摘要。
        private static void DrawSelectionSummary(Rect rect, List<Pawn> candidates, MunoHostageExchangeDraft draft, bool caravanMode)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect inner = rect.ContractedBy(12f);
            float y = inner.y;
            y += DrawMeasuredLabel(new Rect(inner.x, y, inner.width, inner.height), caravanMode ? "缪诺据点批量交换" : "缪诺穿梭机批量接收", GameFont.Medium, MunoCommUIStyle.DarkTextColor) + 10f;
            string rules = "可上交对象：人形成人殖民者、囚犯和奴隶。\n每名人员对应一份奖励。\n进入奖励页后可逐人选择缪诺成员或等值随机物资。";
            y += DrawMeasuredLabel(new Rect(inner.x, y, inner.width, inner.yMax - y), rules, GameFont.Small, MunoCommUIStyle.MutedDarkTextColor) + 12f;

            string selectedText = draft.SelectedCount == 0
                ? "尚未选择任何人员。"
                : "已选择 " + draft.SelectedCount + " 人，可用候选共 " + candidates.Count + " 人。";
            DrawMeasuredLabel(new Rect(inner.x, y, inner.width, inner.yMax - y), selectedText, GameFont.Small, MunoCommUIStyle.DarkTextColor);
        }

        //绘制逐人奖励分配列表和批量奖励命令。
        private static void DrawRewardAssignments(Rect rect, MunoHostageExchangeDraft draft, ref Vector2 scrollPosition)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(10f);
            float lineHeight = Text.LineHeightOf(GameFont.Small) + 4f;
            DrawOneLineLabel(new Rect(inner.x, inner.y, inner.width - 220f, lineHeight), "逐人奖励分配", MunoCommUIStyle.AccentSoftColor);

            Rect allMunoRect = new Rect(inner.xMax - 212f, inner.y, 102f, lineHeight + 4f);
            Rect allItemsRect = new Rect(allMunoRect.xMax + 8f, inner.y, 102f, lineHeight + 4f);
            if (MunoCommUIStyle.DrawButton(allMunoRect, "全部缪诺"))
            {
                draft.SetAllRewardTypes(MunoExchangeRewardType.MunoPawn);
            }
            if (MunoCommUIStyle.DrawButton(allItemsRect, "全部物资"))
            {
                draft.SetAllRewardTypes(MunoExchangeRewardType.RandomItems);
            }

            Rect outRect = new Rect(inner.x, inner.y + lineHeight + 12f, inner.width, inner.height - lineHeight - 12f);
            float viewWidth = outRect.width - 16f;
            float rowHeight = GetPawnRowHeight();
            float viewHeight = draft.SelectedCount * (rowHeight + RowGap) + 2f;
            Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(outRect.height, viewHeight));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            try
            {
                float y = 0f;
                for (int i = 0; i < draft.SelectedPawns.Count; i++)
                {
                    Pawn pawn = draft.SelectedPawns[i];
                    DrawRewardRow(new Rect(0f, y, viewWidth, rowHeight), pawn, draft);
                    y += rowHeight + RowGap;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        //绘制单名目标的奖励类型切换按钮。
        private static void DrawRewardRow(Rect rect, Pawn pawn, MunoHostageExchangeDraft draft)
        {
            Widgets.DrawBoxSolid(rect, new Color(0f, 0f, 0f, 0.08f));
            MunoCommUIStyle.DrawBorder(rect, new Color(0.32f, 0.45f, 0.45f));
            Rect inner = rect.ContractedBy(8f);
            Widgets.ThingIcon(new Rect(inner.x, inner.y + (inner.height - PawnIconSize) * 0.5f, PawnIconSize, PawnIconSize), pawn);
            float lineHeight = Text.LineHeightOf(GameFont.Small) + 2f;
            float buttonWidth = 118f;
            float textWidth = inner.width - 54f - buttonWidth - 10f;
            DrawOneLineLabel(new Rect(inner.x + 54f, inner.y + 2f, textWidth, lineHeight), pawn.LabelCap, MunoCommUIStyle.DarkTextColor);
            DrawOneLineLabel(new Rect(inner.x + 54f, inner.y + lineHeight + 7f, textWidth, lineHeight), "上交价值 " + pawn.MarketValue.ToStringMoney(), MunoCommUIStyle.MutedDarkTextColor);

            MunoExchangeRewardType currentType = draft.GetRewardType(pawn);
            string label = currentType == MunoExchangeRewardType.MunoPawn ? "缪诺成员" : "等值物资";
            Rect buttonRect = new Rect(inner.xMax - buttonWidth, inner.y + (inner.height - 38f) * 0.5f, buttonWidth, 38f);
            if (MunoCommUIStyle.DrawButton(buttonRect, label))
            {
                MunoExchangeRewardType nextType = currentType == MunoExchangeRewardType.MunoPawn
                    ? MunoExchangeRewardType.RandomItems
                    : MunoExchangeRewardType.MunoPawn;
                draft.SetRewardType(pawn, nextType);
            }
        }

        //绘制奖励数量、随机物资价值和具体物资清单。
        private static void DrawRewardPreview(Rect rect, MunoHostageExchangeDraft draft, ref Vector2 scrollPosition, string previewError, bool caravanMode)
        {
            MunoCommUIStyle.DrawLightPanel(rect);
            Rect inner = rect.ContractedBy(12f);
            int munoCount = draft.CountRewardType(MunoExchangeRewardType.MunoPawn);
            int itemTargetCount = draft.CountRewardType(MunoExchangeRewardType.RandomItems);
            float y = inner.y;
            y += DrawMeasuredLabel(new Rect(inner.x, y, inner.width, inner.height), "奖励预览", GameFont.Medium, MunoCommUIStyle.DarkTextColor) + 8f;
            string summary = "缪诺成员：" + munoCount + " 名\n等值物资对应人员：" + itemTargetCount + " 名\n物资价值基准：" + draft.ItemPawnValue.ToStringMoney()
                + "\n生成物资价值：" + draft.ItemRewardMarketValue.ToStringMoney()
                + "\n送达方式：" + (caravanMode ? "直接加入远行队" : "穿梭机离场后空投");
            y += DrawMeasuredLabel(new Rect(inner.x, y, inner.width, inner.yMax - y), summary, GameFont.Small, MunoCommUIStyle.MutedDarkTextColor) + 10f;

            Rect outRect = new Rect(inner.x, y, inner.width, inner.yMax - y);
            if (!previewError.NullOrEmpty())
            {
                DrawMeasuredLabel(outRect, previewError, GameFont.Small, Color.red);
                return;
            }

            if (draft.ItemRewards.Count == 0)
            {
                DrawMeasuredLabel(outRect, "当前没有选择随机物资奖励。", GameFont.Small, MunoCommUIStyle.DarkTextColor);
                return;
            }

            float viewWidth = outRect.width - 16f;
            float lineHeight = Text.LineHeightOf(GameFont.Small) + 6f;
            float viewHeight = draft.ItemRewards.Count * lineHeight + 2f;
            Rect viewRect = new Rect(0f, 0f, viewWidth, Mathf.Max(outRect.height, viewHeight));
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            try
            {
                float itemY = 0f;
                for (int i = 0; i < draft.ItemRewards.Count; i++)
                {
                    Thing item = draft.ItemRewards[i];
                    string label = item.LabelCap + " x" + item.stackCount + "    " + (item.MarketValue * item.stackCount).ToStringMoney();
                    DrawOneLineLabel(new Rect(0f, itemY, viewWidth, lineHeight), label, MunoCommUIStyle.DarkTextColor);
                    itemY += lineHeight;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        //根据实际字体行高和 Pawn 图标尺寸计算候选行高度。
        private static float GetPawnRowHeight()
        {
            float lineHeight = Text.LineHeightOf(GameFont.Small) + 2f;
            float textHeight = lineHeight * 2f + 7f;
            float innerHeight = Mathf.Max(PawnIconSize, textHeight);
            return innerHeight + 16f;
        }

        //绘制不会泄漏字体和颜色状态的单行文本。
        private static void DrawOneLineLabel(Rect rect, string text, Color color)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                GUI.color = color;
                Widgets.Label(rect, text);
                TooltipHandler.TipRegion(rect, text);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        //测量并绘制可换行文本，同时返回实际占用高度。
        private static float DrawMeasuredLabel(Rect area, string text, GameFont font, Color color)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = font;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = color;
                float height = Mathf.Ceil(Text.CalcHeight(text, area.width)) + 2f;
                Widgets.Label(new Rect(area.x, area.y, area.width, height), text);
                return height;
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        //在空列表区域居中绘制提示文本并恢复全局 GUI 状态。
        private static void DrawCenteredLabel(Rect rect, string text)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = true;
                GUI.color = MunoCommUIStyle.DarkTextColor;
                Widgets.Label(rect, text);
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }
    }
}
