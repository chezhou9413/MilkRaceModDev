using MunoRaceLib.MunoComp;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MunoRaceLib.MunoGizmo
{
    /// <summary>
    /// 以原版血源条风格显示乳源质存量，并允许直接拖拽调整自动收集阈值。
    /// </summary>
    [StaticConstructorOnStartup]
    public class Gizmo_GalactogenBar : Gizmo_Slider
    {
        public static readonly float[] ThresholdPresets = { 0.2f, 0.4f, 0.6f, 0.8f, 1f };
        private const float SnapDistance = 0.035f;
        private static readonly Color MilkyWhite = new Color(0.95f, 0.95f, 0.9f);
        private static readonly Color MilkyHighlight = new Color(1f, 1f, 0.96f);
        private static readonly Color DraggingColor = new Color(0.3f, 0.6f, 1f);
        private readonly Pawn pawn;
        private readonly ThingComp_Galactogen comp;
        private static bool draggingBar;

        /// <summary>
        /// 为指定小人创建一个采用原版资源条布局的乳源质阈值调节 Gizmo。
        /// </summary>
        public Gizmo_GalactogenBar(Pawn pawn)
        {
            this.pawn = pawn;
            comp = pawn.GetComp<ThingComp_Galactogen>();
        }

        /// <summary>
        /// 返回当前乳源质组件，供资源条显示与阈值回写使用。
        /// </summary>
        private ThingComp_Galactogen Comp => comp ?? pawn.GetComp<ThingComp_Galactogen>();

        /// <summary>
        /// 返回资源条目标阈值，并将拖拽结果写回自动收集阈值。
        /// </summary>
        protected override float Target
        {
            get => Comp.AutoGather;
            set => Comp.AutoGather = SnapToPreset(value);
        }

        /// <summary>
        /// 返回当前乳源质占最大容量的百分比。
        /// </summary>
        protected override float ValuePercent => Mathf.Clamp01(Comp.CurrentGalactogen / Math.Max(1f, Comp.MaxGalactogen));

        /// <summary>
        /// 返回资源条填充颜色。
        /// </summary>
        protected override Color BarColor => MilkyWhite;

        /// <summary>
        /// 返回鼠标悬停时的高亮颜色。
        /// </summary>
        protected override Color BarHighlightColor => MilkyHighlight;

        /// <summary>
        /// 返回拖拽目标线的颜色。
        /// </summary>
        protected override Color BarDragColor => DraggingColor;

        /// <summary>
        /// 返回可拖拽阈值范围。
        /// </summary>
        protected override FloatRange DragRange => FloatRange.ZeroToOne;

        /// <summary>
        /// 告知原版滑条本 Gizmo 需要启用拖拽阈值交互。
        /// </summary>
        protected override bool IsDraggable => true;

        /// <summary>
        /// 返回资源条中心显示的当前数值。
        /// </summary>
        protected override string BarLabel => $"{Comp.CurrentGalactogen:F0} / {Comp.MaxGalactogen:F0}";

        /// <summary>
        /// 返回资源条标题文本。
        /// </summary>
        protected override string Title => Comp.Props.GalactogenUIName.CapitalizeFirst();

        /// <summary>
        /// 返回离散拖拽步进数量，保持和原版血源条接近的手感。
        /// </summary>
        protected override int Increments => 20;

        /// <summary>
        /// 返回资源条固定宽度，保持和原版资源类 Gizmo 接近。
        /// </summary>
        protected override float Width => 212f;

        /// <summary>
        /// 统一维护当前是否正在拖拽阈值，并在释放时播放确认音效。
        /// </summary>
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

        /// <summary>
        /// 返回条内需要绘制的常用阈值刻度，方便快速拖到常见区间。
        /// </summary>
        protected override IEnumerable<float> GetBarThresholds()
        {
            for (int i = 0; i < ThresholdPresets.Length; i++)
            {
                yield return ThresholdPresets[i];
            }
        }

        /// <summary>
        /// 绘制表头时同时显示资源名和当前阈值百分比，减少额外按钮占位。
        /// </summary>
        protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(headerRect.x, headerRect.y, Mathf.Max(0f, headerRect.width - 52f), headerRect.height), Title);

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(headerRect, $"{Comp.AutoGather * 100f:F0}%");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// 返回悬浮提示，说明当前阈值和拖拽方式。
        /// </summary>
        protected override string GetTooltip()
        {
            return $"{Comp.Props.GalactogenUIDes}\n\n当前自动收集阈值：{Comp.AutoGather * 100f:F0}%\n可以直接拖动条内目标线进行快速调整，靠近常用刻度时会自动吸附。";
        }

        /// <summary>
        /// 供外部逻辑直接应用预设阈值。
        /// </summary>
        public static void ApplyThresholdPreset(ThingComp_Galactogen comp, float preset)
        {
            if (comp == null)
            {
                return;
            }

            comp.AutoGather = SnapToPreset(preset);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        /// <summary>
        /// 将拖拽得到的阈值吸附到附近的常用刻度，便于快速回到固定节点。
        /// </summary>
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
