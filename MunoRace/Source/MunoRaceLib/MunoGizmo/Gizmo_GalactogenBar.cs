using MunoRaceLib.MunoComp;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MunoRaceLib.MunoGizmo
{
    //以原版血源条风格显示乳源质存量，并负责阈值拖拽与数值对比色绘制。
    [StaticConstructorOnStartup]
    public class Gizmo_GalactogenBar : Gizmo_Slider
    {
        public static readonly float[] ThresholdPresets = { 0.2f, 0.4f, 0.6f, 0.8f, 1f };
        private const float SnapDistance = 0.035f;
        private const string FilledLabelColor = "#4A4A4A";
        private const string EmptyLabelColor = "#F2F2F2";
        private static readonly Color MilkyWhite = new Color(0.95f, 0.95f, 0.9f);
        private static readonly Color MilkyHighlight = new Color(1f, 1f, 0.96f);
        private static readonly Color DraggingColor = new Color(0.3f, 0.6f, 1f);
        private readonly Pawn pawn;
        private readonly ThingComp_Galactogen comp;
        private static bool draggingBar;

        //为指定小人创建采用原版资源条布局的乳源质阈值调节 Gizmo。
        public Gizmo_GalactogenBar(Pawn pawn)
        {
            this.pawn = pawn;
            comp = pawn.GetComp<ThingComp_Galactogen>();
        }

        //返回当前乳源质组件，供资源条显示与阈值回写使用。
        private ThingComp_Galactogen Comp => comp ?? pawn.GetComp<ThingComp_Galactogen>();

        //返回资源条目标阈值，并将拖拽结果写回自动收集阈值。
        protected override float Target
        {
            get => Comp.AutoGather;
            set => Comp.AutoGather = SnapToPreset(value);
        }

        //返回当前乳源质占最大容量的百分比。
        protected override float ValuePercent => Mathf.Clamp01(Comp.CurrentGalactogen / Math.Max(1f, Comp.MaxGalactogen));

        //返回资源条填充颜色。
        protected override Color BarColor => MilkyWhite;

        //返回鼠标悬停时的高亮颜色。
        protected override Color BarHighlightColor => MilkyHighlight;

        //返回拖拽目标线的颜色。
        protected override Color BarDragColor => DraggingColor;

        //返回可拖拽阈值范围。
        protected override FloatRange DragRange => FloatRange.ZeroToOne;

        //告知原版滑条本 Gizmo 需要启用拖拽阈值交互。
        protected override bool IsDraggable => true;

        //返回按乳白填充区和深色空槽区自动分色的当前数值。
        protected override string BarLabel => BuildContrastingBarLabel($"{Comp.CurrentGalactogen:F0} / {Comp.MaxGalactogen:F0}");

        //返回资源条标题文本。
        protected override string Title => Comp.Props.GalactogenUIName.CapitalizeFirst();

        //返回离散拖拽步进数量，保持和原版血源条接近的手感。
        protected override int Increments => 20;

        //返回资源条固定宽度，保持和原版资源类 Gizmo 接近。
        protected override float Width => 212f;

        //统一维护当前是否正在拖拽阈值，并在释放时播放确认音效。
        protected override bool DraggingBar
        {
            get => draggingBar;
            set
            {
                if (draggingBar && !value)
                {
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                }

                draggingBar = value;
            }
        }

        //绘制资源条并在结束时恢复 RimWorld 全局 IMGUI 状态。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth, parms);
                RegisterBarTooltip();
                return result;
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
                GUI.color = oldColor;
            }
        }

        //返回条内需要绘制的常用阈值刻度，方便快速拖到常见区间。
        protected override IEnumerable<float> GetBarThresholds()
        {
            for (int i = 0; i < ThresholdPresets.Length; i++)
            {
                yield return ThresholdPresets[i];
            }
        }

        //绘制资源名和当前阈值百分比，并保持调用前的文字对齐状态。
        protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
        {
            TextAnchor oldAnchor = Text.Anchor;
            try
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(new Rect(headerRect.x, headerRect.y, Mathf.Max(0f, headerRect.width - 52f), headerRect.height), Title);

                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(headerRect, $"{Comp.AutoGather * 100f:F0}%");
                mouseOverElement = true;
            }
            finally
            {
                Text.Anchor = oldAnchor;
            }
        }

        //返回悬浮提示，说明当前阈值和拖拽方式。
        protected override string GetTooltip()
        {
            string recovery = Comp.RecoveryPerHour.ToString("0.##");
            string equipmentDrain = Comp.EquipmentDrainPerHour.ToString("0.##");
            return $"{Comp.Props.GalactogenUIDes}\n\n乳源质恢复：+{recovery} / 小时\n装备消耗：-{equipmentDrain} / 小时\n当前自动收集阈值：{Comp.AutoGather * 100f:F0}%\n可以直接拖动条内目标线进行快速调整，靠近常用刻度时会自动吸附。";
        }

        //在乳源质进度条矩形上直接注册动态悬浮提示，避免依赖父类整块 Gizmo 的提示区域。
        private void RegisterBarTooltip()
        {
            if (!Mouse.IsOver(barRect))
            {
                return;
            }

            Widgets.DrawHighlight(barRect);
            TipSignal tip = new TipSignal(GetTooltip, Gen.HashCombineInt(pawn.thingIDNumber, 48291037));
            tip.delay = 0.1f;
            TooltipHandler.TipRegion(barRect, tip);
        }

        //供外部逻辑直接应用预设阈值。
        public static void ApplyThresholdPreset(ThingComp_Galactogen comp, float preset)
        {
            if (comp == null)
            {
                return;
            }

            comp.AutoGather = SnapToPreset(preset);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        //根据填充终点与每个字符中心的位置，把填充区文字设为深灰、空槽区文字设为浅灰。
        private string BuildContrastingBarLabel(string label)
        {
            if (label.NullOrEmpty() || barRect.width <= 6f)
            {
                return ColorizeBarLabel(label, EmptyLabelColor);
            }

            float labelWidth = Text.CalcSize(label).x;
            float labelLeft = barRect.center.x - labelWidth * 0.5f;
            float fillRight = barRect.x + 3f + (barRect.width - 6f) * ValuePercent;
            float previousPrefixWidth = 0f;
            int filledCharacterCount = 0;

            //以字符中心落在哪一侧为准，避免填充边界穿过数字时整段文字同时失去对比度。
            for (int i = 0; i < label.Length; i++)
            {
                float prefixWidth = Text.CalcSize(label.Substring(0, i + 1)).x;
                float characterCenter = labelLeft + (previousPrefixWidth + prefixWidth) * 0.5f;
                if (characterCenter > fillRight)
                {
                    break;
                }

                filledCharacterCount = i + 1;
                previousPrefixWidth = prefixWidth;
            }

            if (filledCharacterCount == 0)
            {
                return ColorizeBarLabel(label, EmptyLabelColor);
            }

            if (filledCharacterCount == label.Length)
            {
                return ColorizeBarLabel(label, FilledLabelColor);
            }

            string filledText = label.Substring(0, filledCharacterCount);
            string emptyText = label.Substring(filledCharacterCount);
            return ColorizeBarLabel(filledText, FilledLabelColor) + ColorizeBarLabel(emptyText, EmptyLabelColor);
        }

        //用 Unity 富文本颜色标记包装资源条数值片段。
        private static string ColorizeBarLabel(string text, string htmlColor)
        {
            return $"<color={htmlColor}>{text}</color>";
        }

        //将拖拽得到的阈值吸附到附近的常用刻度，便于快速回到固定节点。
        private static float SnapToPreset(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            for (int i = 0; i < ThresholdPresets.Length; i++)
            {
                float preset = ThresholdPresets[i];
                if (Mathf.Abs(clampedValue - preset) <= SnapDistance)
                {
                    return preset;
                }
            }

            return clampedValue;
        }
    }
}
