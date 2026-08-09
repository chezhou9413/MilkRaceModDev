using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    //负责统一管理缪诺通讯交换界面的配色、边框与按钮绘制样式。
    [StaticConstructorOnStartup]
    public static class MunoCommUIStyle
    {
        public static readonly Color BackgroundColor = new Color(0.05f, 0.08f, 0.09f);
        public static readonly Color PanelColor = new Color(0.12f, 0.17f, 0.18f);
        public static readonly Color SoftPanelColor = new Color(0.17f, 0.25f, 0.25f);
        public static readonly Color AccentColor = new Color(0.22f, 0.72f, 0.68f);
        public static readonly Color AccentSoftColor = new Color(0.58f, 0.86f, 0.82f);
        public static readonly Color GoldColor = new Color(0.92f, 0.82f, 0.55f);
        public static readonly Color BorderColor = new Color(0.28f, 0.56f, 0.54f);
        public static readonly Color TextColor = new Color(0.93f, 0.96f, 0.95f);
        public static readonly Color SubtleTextColor = new Color(0.72f, 0.82f, 0.81f);
        public static readonly Color DarkTextColor = new Color(0.91f, 0.96f, 0.95f);
        public static readonly Color MutedDarkTextColor = new Color(0.70f, 0.82f, 0.80f);
        public static readonly Color DisabledColor = new Color(0.35f, 0.40f, 0.40f);
        public static readonly Texture2D CloseXSmall = ContentFinder<Texture2D>.Get("UI/Widgets/CloseXSmall", true);

        //根据窗口创建时间计算页面进入动效后的绘制区域。
        public static Rect ApplyEntryAnimation(Rect rect, float openTime)
        {
            float elapsed = Mathf.Clamp01((Time.realtimeSinceStartup - openTime) / 0.22f);
            float eased = 1f - Mathf.Pow(1f - elapsed, 3f);
            float offsetY = Mathf.Lerp(16f, 0f, eased);
            return new Rect(rect.x, rect.y + offsetY, rect.width, rect.height);
        }

        //根据窗口创建时间返回页面进入动效透明度。
        public static float EntryAlpha(float openTime)
        {
            float elapsed = Mathf.Clamp01((Time.realtimeSinceStartup - openTime) / 0.22f);
            return 1f - Mathf.Pow(1f - elapsed, 2f);
        }

        //绘制整窗背景与外层描边。
        public static void DrawBackground(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, BackgroundColor);
            DrawBorder(rect, BorderColor, 2);
        }

        //绘制通讯终端标题栏，并返回右侧关闭按钮是否被点击。
        public static bool DrawTerminalHeader(Rect inRect, string title, Texture2D logo)
        {
            Rect headerRect = new Rect(inRect.x + 12f, inRect.y + 10f, inRect.width - 24f, 42f);
            DrawPanel(headerRect);

            Rect logoRect = new Rect(headerRect.x + 10f, headerRect.y + 6f, 30f, 30f);
            if (logo != null)
            {
                GUI.DrawTexture(logoRect, logo);
            }

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.WordWrap = false;
            GUI.color = GoldColor;
            Widgets.Label(new Rect(logoRect.xMax + 10f, headerRect.y + 4f, headerRect.width - 90f, 34f), title);
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            Text.WordWrap = oldWordWrap;
            GUI.color = oldColor;

            Rect closeRect = new Rect(headerRect.xMax - 34f, headerRect.y + 6f, 28f, 28f);
            return Widgets.ButtonImage(closeRect, CloseXSmall);
        }

        //绘制深色信息面板，用于正文和立绘承载区。
        public static void DrawPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, PanelColor);
            DrawBorder(rect, BorderColor, 1);
        }

        //绘制浅色重点卡片，用于承载说明摘要与选中目标信息。
        public static void DrawLightPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, SoftPanelColor);
            DrawBorder(rect, AccentColor, 1);
            Widgets.DrawBoxSolid(rect.ContractedBy(1f), new Color(1f, 1f, 1f, 0.015f));
        }

        //为指定区域绘制描边，避免窗口层级在深色背景中混成一片。
        public static void DrawBorder(Rect rect, Color color, int thickness = 1)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawBox(rect, thickness);
            GUI.color = oldColor;
        }

        //绘制缪诺风格按钮，并在启用时返回点击结果。
        public static bool DrawButton(Rect rect, string label, bool active = true)
        {
            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            bool hovered = active && Mouse.IsOver(rect);
            bool pressed = hovered && Event.current.type == EventType.MouseDown && Event.current.button == 0;
            Rect drawRect = pressed ? rect.ContractedBy(2f) : rect;
            Color bgColor = active ? new Color(0.10f, 0.24f, 0.24f) : new Color(0.12f, 0.14f, 0.14f);
            Color borderColor = active ? AccentColor : DisabledColor;
            Color textColor = active ? TextColor : new Color(0.64f, 0.66f, 0.66f);

            Widgets.DrawBoxSolid(drawRect, bgColor);
            DrawBorder(drawRect, hovered ? AccentSoftColor : borderColor, hovered ? 2 : 1);
            if (hovered)
            {
                Widgets.DrawBoxSolid(drawRect, new Color(0.35f, 0.92f, 0.84f, pressed ? 0.22f : 0.14f));
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Text.WordWrap = false;
            GUI.color = textColor;
            Widgets.Label(drawRect, label);
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            Text.WordWrap = oldWordWrap;
            GUI.color = oldColor;
            return active && Widgets.ButtonInvisible(drawRect);
        }

    }
}
