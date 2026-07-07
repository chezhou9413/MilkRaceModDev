using RimWorld;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //表示缪诺通讯首页在本帧触发的用户动作。
    public enum MunoCommMainMenuAction
    {
        None,
        OpenMilitaryExchange,
        OpenExchange,
        OpenLogistics,
        ForceRefreshMarriage,
        Close
    }
    //负责绘制缪诺通讯首页的入口菜单、问候面板与联络员立绘。
    [StaticConstructorOnStartup]
    public static class MunoCommMainMenuView
    {
        private const float DefaultPortraitColumnWidth = 330f;
        private const string DefaultPortraitPath = "UI/MunoCommManagers";
        //绘制首页并返回本帧触发的页面动作。
        public static MunoCommMainMenuAction Draw(Rect inRect, float openTime, MunoTypewriterTextState typewriter, string portraitTexPath = null)
        {
            return Draw(inRect, null, openTime, typewriter, portraitTexPath, true);
        }
        //绘制旧通讯流程首页，并保留自定义管理员入口。
        public static MunoCommMainMenuAction Draw(Rect inRect, string managerButtonLabel, float openTime, MunoTypewriterTextState typewriter, string portraitTexPath = null)
        {
            return Draw(inRect, managerButtonLabel, openTime, typewriter, portraitTexPath, true);
        }
        //根据通讯流程绘制首页，并返回本帧触发的页面动作。
        private static MunoCommMainMenuAction Draw(Rect inRect, string managerButtonLabel, float openTime, MunoTypewriterTextState typewriter, string portraitTexPath, bool militaryOpensExchange)
        {
            Color oldGuiColor = GUI.color;
            float alpha = MunoCommUIStyle.EntryAlpha(openTime);
            GUI.color = new Color(oldGuiColor.r, oldGuiColor.g, oldGuiColor.b, oldGuiColor.a * alpha);
            inRect = MunoCommUIStyle.ApplyEntryAnimation(inRect, openTime);

            DrawPortrait(inRect, portraitTexPath);
            DrawGreetingPanel(GetGreetingPanelRect(inRect), typewriter);
            MunoCommMainMenuAction action = DrawHelpPanel(GetHelpPanelRect(inRect, managerButtonLabel), managerButtonLabel);
            if (action != MunoCommMainMenuAction.None)
            {
                GUI.color = oldGuiColor;
                return action;
            }

            action = DrawDisconnectButton(inRect);
            GUI.color = oldGuiColor;
            return action;
        }
        //计算首页问候面板的位置，使其与右侧菜单区域保持固定间距。
        private static Rect GetGreetingPanelRect(Rect inRect)
        {
            float portraitColumnWidth = MunoCommUIConfigUtility.ColumnWidth(MunoCommUIConfigUtility.HomePortrait(), DefaultPortraitColumnWidth);
            float rightX = inRect.x + portraitColumnWidth + 36f;
            float rightWidth = Mathf.Max(360f, inRect.xMax - rightX - 24f);
            return new Rect(rightX, inRect.y + 18f, rightWidth, 220f);
        }
        //计算首页管理员选择面板的位置，使其承接问候面板并保留下方断开按钮区域。
        private static Rect GetHelpPanelRect(Rect inRect, string managerButtonLabel)
        {
            Rect greetingRect = GetGreetingPanelRect(inRect);
            float height = DebugSettings.godMode ? 224f : 170f;
            if (!managerButtonLabel.NullOrEmpty())
            {
                height += 54f;
            }

            return new Rect(greetingRect.x, greetingRect.yMax + 18f, greetingRect.width, height);
        }
        //绘制首页左侧联络员立绘与身份标签。
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
        //计算首页立绘面板区域，并确保底部身份标签不会嵌入立绘框。
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
        //绘制首页缪诺招待链路面板，并展示 XML 配置的随机问候文本。
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
        //绘制首页管理员入口面板，并返回对应入口的动作。
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
                if (!managerButtonLabel.NullOrEmpty())
                {
                    return MunoCommMainMenuAction.OpenMilitaryExchange;
                }

                return MunoCommMainMenuAction.OpenExchange;
            }

            if (MunoCommUIStyle.DrawButton(new Rect(inner.x, buttonY + 54f, buttonWidth, buttonHeight), "后勤管理员"))
            {
                return MunoCommMainMenuAction.OpenLogistics;
            }

            float debugY = buttonY + 108f;
            if (!managerButtonLabel.NullOrEmpty())
            {
                if (MunoCommUIStyle.DrawButton(new Rect(inner.x, debugY, buttonWidth, buttonHeight), managerButtonLabel))
                {
                    return MunoCommMainMenuAction.OpenExchange;
                }

                debugY += 54f;
            }

            if (DebugSettings.godMode)
            {
                Rect refreshRect = new Rect(inner.x, debugY, buttonWidth, buttonHeight);
                if (MunoCommUIStyle.DrawButton(refreshRect, "立刻刷新和亲"))
                {
                    return MunoCommMainMenuAction.ForceRefreshMarriage;
                }
            }

            return MunoCommMainMenuAction.None;
        }
        //绘制首页底部断开通讯文字按钮并返回关闭动作。
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
