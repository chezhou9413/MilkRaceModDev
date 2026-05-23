using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责统一管理缪诺通讯交换界面的配色、边框与按钮绘制样式。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MunoCommUIStyle
    {
        public static readonly Color BackgroundColor = new Color(0.05f, 0.08f, 0.09f);
        public static readonly Color PanelColor = new Color(0.12f, 0.17f, 0.18f);
        public static readonly Color SoftPanelColor = new Color(0.64f, 0.70f, 0.70f);
        public static readonly Color AccentColor = new Color(0.22f, 0.72f, 0.68f);
        public static readonly Color AccentSoftColor = new Color(0.58f, 0.86f, 0.82f);
        public static readonly Color GoldColor = new Color(0.92f, 0.82f, 0.55f);
        public static readonly Color BorderColor = new Color(0.28f, 0.56f, 0.54f);
        public static readonly Color TextColor = new Color(0.93f, 0.96f, 0.95f);
        public static readonly Color SubtleTextColor = new Color(0.72f, 0.82f, 0.81f);
        public static readonly Color DarkTextColor = new Color(0.08f, 0.13f, 0.13f);
        public static readonly Color MutedDarkTextColor = new Color(0.16f, 0.24f, 0.24f);
        public static readonly Color DisabledColor = new Color(0.35f, 0.40f, 0.40f);
        public static readonly Texture2D CloseXSmall = ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);

        /// <summary>
        /// 绘制整窗背景与外层描边。
        /// </summary>
        public static void DrawBackground(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, BackgroundColor);
            DrawBorder(rect, BorderColor, 2);
        }

        /// <summary>
        /// 绘制深色信息面板，用于正文和立绘承载区。
        /// </summary>
        public static void DrawPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, PanelColor);
            DrawBorder(rect, BorderColor, 1);
        }

        /// <summary>
        /// 绘制浅色重点卡片，用于承载说明摘要与选中目标信息。
        /// </summary>
        public static void DrawLightPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, SoftPanelColor);
            DrawBorder(rect, AccentColor, 1);
            Widgets.DrawBoxSolid(rect.ContractedBy(1f), new Color(1f, 1f, 1f, 0.03f));
        }

        /// <summary>
        /// 为指定区域绘制描边，避免窗口层级在深色背景中混成一片。
        /// </summary>
        public static void DrawBorder(Rect rect, Color color, int thickness = 1)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawBox(rect, thickness);
            GUI.color = oldColor;
        }

        /// <summary>
        /// 绘制缪诺风格按钮，并在启用时返回点击结果。
        /// </summary>
        public static bool DrawButton(Rect rect, string label, bool active = true)
        {
            Color oldColor = GUI.color;
            Color bgColor = active ? new Color(0.10f, 0.24f, 0.24f) : new Color(0.12f, 0.14f, 0.14f);
            Color borderColor = active ? AccentColor : DisabledColor;
            Color textColor = active ? TextColor : new Color(0.64f, 0.66f, 0.66f);

            Widgets.DrawBoxSolid(rect, bgColor);
            DrawBorder(rect, borderColor, 1);
            if (active && Mouse.IsOver(rect))
            {
                Widgets.DrawBoxSolid(rect, new Color(0.35f, 0.92f, 0.84f, 0.14f));
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = textColor;
            Widgets.Label(rect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = oldColor;
            return active && Widgets.ButtonInvisible(rect);
        }
    }
}
