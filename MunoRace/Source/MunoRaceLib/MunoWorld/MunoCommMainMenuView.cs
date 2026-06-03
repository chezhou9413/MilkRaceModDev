using RimWorld;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 表示缪诺通讯首页在本帧触发的用户动作。
    /// </summary>
    public enum MunoCommMainMenuAction
    {
        None,
        OpenExchange,
        ForceRefreshMarriage,
        Close
    }

    /// <summary>
    /// 负责绘制缪诺通讯首页的入口菜单、问候面板与联络员立绘。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MunoCommMainMenuView
    {
        private const float DefaultPortraitColumnWidth = 330f;
        private const string DefaultPortraitPath = "UI/MunoCommHomePortrait";

        /// <summary>
        /// 绘制首页并返回本帧触发的页面动作。
        /// </summary>
        public static MunoCommMainMenuAction Draw(Rect inRect, string managerButtonLabel, float openTime, MunoTypewriterTextState typewriter, string portraitTexPath = null)
        {
            Color oldGuiColor = GUI.color;
            float alpha = MunoCommUIStyle.EntryAlpha(openTime);
            GUI.color = new Color(oldGuiColor.r, oldGuiColor.g, oldGuiColor.b, oldGuiColor.a * alpha);
            inRect = MunoCommUIStyle.ApplyEntryAnimation(inRect, openTime);

            DrawPortrait(inRect, portraitTexPath);
            DrawGreetingPanel(GetGreetingPanelRect(inRect), typewriter);
            MunoCommMainMenuAction action = DrawHelpPanel(GetHelpPanelRect(inRect), managerButtonLabel);
            if (action != MunoCommMainMenuAction.None)
            {
                GUI.color = oldGuiColor;
                return action;
            }

            action = DrawDisconnectButton(inRect);
            GUI.color = oldGuiColor;
            return action;
        }

        /// <summary>
        /// 计算首页问候面板的位置，使其与右侧菜单区域保持固定间距。
        /// </summary>
        private static Rect GetGreetingPanelRect(Rect inRect)
        {
            float portraitColumnWidth = MunoCommUIConfigUtility.ColumnWidth(MunoCommUIConfigUtility.HomePortrait(), DefaultPortraitColumnWidth);
            float rightX = inRect.x + portraitColumnWidth + 36f;
            float rightWidth = Mathf.Max(360f, inRect.xMax - rightX - 24f);
            return new Rect(rightX, inRect.y + 18f, rightWidth, 220f);
        }

        /// <summary>
        /// 计算首页管理员选择面板的位置，使其承接问候面板并保留下方断开按钮区域。
        /// </summary>
        private static Rect GetHelpPanelRect(Rect inRect)
        {
            Rect greetingRect = GetGreetingPanelRect(inRect);
            float height = DebugSettings.godMode ? 278f : 224f;
            return new Rect(greetingRect.x, greetingRect.yMax + 18f, greetingRect.width, height);
        }

        /// <summary>
        /// 绘制首页左侧联络员立绘与身份标签。
        /// </summary>
        private static void DrawPortrait(Rect inRect, string portraitTexPath)
        {
            MunoCommPortraitLayout layout = MunoCommUIConfigUtility.WithPortraitPath(MunoCommUIConfigUtility.HomePortrait(), portraitTexPath);
            Rect outerRect = GetPortraitPanelRect(inRect, layout);
            MunoCommUIConfigUtility.DrawPortraitPanel(outerRect, layout, DefaultPortraitPath);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            float labelHeight = Text.LineHeightOf(GameFont.Medium) + 12f;
            Rect labelRect = new Rect(outerRect.x + 18f, outerRect.yMax + 14f, outerRect.width - 36f, labelHeight);
            MunoCommUIStyle.DrawLightPanel(labelRect);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = MunoCommUIStyle.TextColor;
            Widgets.Label(labelRect, "聚落管理员");
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }

        /// <summary>
        /// 计算首页立绘面板区域，并确保底部身份标签不会嵌入立绘框。
        /// </summary>
        private static Rect GetPortraitPanelRect(Rect inRect, MunoCommPortraitLayout layout)
        {
            float portraitColumnWidth = MunoCommUIConfigUtility.ColumnWidth(layout, DefaultPortraitColumnWidth);
            Rect outerRect = MunoCommUIConfigUtility.PanelRect(inRect, layout, 18f, 18f, portraitColumnWidth - 18f, inRect.height - 112f);
            float labelHeight = Text.LineHeightOf(GameFont.Medium) + 12f;
            float maxPanelBottom = inRect.yMax - labelHeight - 28f;
            if (outerRect.yMax > maxPanelBottom)
            {
                outerRect.height = Mathf.Max(220f, maxPanelBottom - outerRect.y);
            }

            return outerRect;
        }

        /// <summary>
        /// 绘制首页缪诺招待链路面板，并展示 XML 配置的随机问候文本。
        /// </summary>
        private static void DrawGreetingPanel(Rect rect, MunoTypewriterTextState typewriter)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(18f);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = MunoCommUIStyle.AccentSoftColor;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, Text.LineHeight + 4f), "缪诺招待链路");

            GUI.color = MunoCommUIStyle.TextColor;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Rect textRect = new Rect(inner.x, inner.y + Text.LineHeightOf(GameFont.Medium) + 14f, inner.width, inner.height - Text.LineHeightOf(GameFont.Medium) - 14f);
            Widgets.Label(textRect, typewriter?.VisibleText() ?? string.Empty);
            if (Widgets.ButtonInvisible(textRect))
            {
                typewriter?.Complete();
            }

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
        }

        /// <summary>
        /// 绘制首页管理员入口面板，并返回对应入口的动作。
        /// </summary>
        private static MunoCommMainMenuAction DrawHelpPanel(Rect rect, string managerButtonLabel)
        {
            MunoCommUIStyle.DrawPanel(rect);
            Rect inner = rect.ContractedBy(18f);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = MunoCommUIStyle.AccentSoftColor;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, Text.LineHeight + 4f), "有什么能帮到你吗");
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;

            float buttonY = inner.y + Text.LineHeight + 18f;
            float buttonWidth = Mathf.Min(330f, inner.width);
            float buttonHeight = Mathf.Max(38f, Text.LineHeightOf(GameFont.Medium) + 8f);
            if (MunoCommUIStyle.DrawButton(new Rect(inner.x, buttonY, buttonWidth, buttonHeight), "军事管理员"))
            {
                Messages.Message("军事管理员通讯暂未开放。", MessageTypeDefOf.NeutralEvent, false);
            }

            if (MunoCommUIStyle.DrawButton(new Rect(inner.x, buttonY + 54f, buttonWidth, buttonHeight), "后勤管理员"))
            {
                Messages.Message("后勤管理员通讯暂未开放。", MessageTypeDefOf.NeutralEvent, false);
            }

            if (MunoCommUIStyle.DrawButton(new Rect(inner.x, buttonY + 108f, buttonWidth, buttonHeight), managerButtonLabel))
            {
                return MunoCommMainMenuAction.OpenExchange;
            }

            if (DebugSettings.godMode)
            {
                Rect refreshRect = new Rect(inner.x, buttonY + 162f, buttonWidth, buttonHeight);
                if (MunoCommUIStyle.DrawButton(refreshRect, "立刻刷新和亲"))
                {
                    return MunoCommMainMenuAction.ForceRefreshMarriage;
                }
            }

            return MunoCommMainMenuAction.None;
        }

        /// <summary>
        /// 绘制首页底部断开通讯文字按钮并返回关闭动作。
        /// </summary>
        private static MunoCommMainMenuAction DrawDisconnectButton(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;

            float height = Text.LineHeight + 10f;
            float portraitColumnWidth = MunoCommUIConfigUtility.ColumnWidth(MunoCommUIConfigUtility.HomePortrait(), DefaultPortraitColumnWidth);
            Rect buttonRect = new Rect(inRect.x + portraitColumnWidth + 36f, inRect.yMax - 58f, 180f, Mathf.Max(38f, height));
            bool clicked = MunoCommUIStyle.DrawButton(buttonRect, "断开通讯");

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            GUI.color = oldColor;
            return clicked ? MunoCommMainMenuAction.Close : MunoCommMainMenuAction.None;
        }

    }
}
