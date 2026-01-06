using MunoRaceLib.MunoComp;
using System;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace MunoRaceLib.MunoGizmo
{
    [StaticConstructorOnStartup]
    public class Gizmo_GalactogenBar : Gizmo
    {
        public Pawn pawn;
        public ThingComp_Galactogen comp;
        private static readonly Color MilkyWhite = new Color(0.95f, 0.95f, 0.9f);
        private static readonly Color EmptyBarColor = new Color(0.03f, 0.035f, 0.05f);
        private static readonly Color ThresholdLineColor = new Color(1f, 0.9f, 0f);
        private static readonly Color DraggingColor = new Color(0.3f, 0.6f, 1f);
        private const float TotalPulsateTime = 0.85f;
        private static readonly Texture2D BarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(1f, 1f, 1f, 0.2f));

        private static bool isDraggingAnyThreshold = false;

        public Gizmo_GalactogenBar(Pawn pawn)
        {
            this.pawn = pawn;
            this.Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 212f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (comp == null) comp = pawn.GetComp<ThingComp_Galactogen>();

            //基础框体
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            Rect contentRect = rect.ContractedBy(10f);
            //字体大小
            Text.Font = GameFont.Small;
            float labelHeight = Text.LineHeight;
            Rect labelRect = new Rect(contentRect.x, contentRect.y, contentRect.width, labelHeight);

            //白色
            GUI.color = Color.white;

            //左侧资源名称
            Text.Anchor = TextAnchor.UpperLeft;
            //右侧资源数量
            Widgets.Label(labelRect, comp.Props.GalactogenUIName.CapitalizeFirst());
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(labelRect, $"{comp.CurrentGalactogen:F0} / {comp.MaxGalactogen:F0}");

            //进度条纵向大小
            float barHeight = 32f;
            float barTopMargin = labelHeight + 2f;
            Rect barRect = new Rect(contentRect.x, contentRect.y + barTopMargin, contentRect.width, barHeight);

            //绘制背景
            Widgets.DrawBoxSolid(barRect, EmptyBarColor);

            //绘制填充条
            float fillPct = Mathf.Clamp01(comp.CurrentGalactogen / Math.Max(1f, comp.MaxGalactogen));
            Widgets.FillableBar(barRect, fillPct, SolidColorMaterials.NewSolidColorTexture(MilkyWhite), BaseContent.ClearTex, false);

            //呼吸动画逻辑
            float pulseAlpha = CalculatePulseAlpha();
            if (Mouse.IsOver(barRect))
            {
                GUI.color = new Color(1f, 1f, 1f, pulseAlpha * 0.4f);
                GenUI.DrawTextureWithMaterial(barRect.ContractedBy(1f), BarHighlightTex, null);
                GUI.color = Color.white;
            }

            //绘制阈值线
            HandleInteraction(barRect);
            float thresholdX = barRect.x + barRect.width * comp.AutoGather;
            thresholdX = Mathf.Clamp(thresholdX, barRect.x, barRect.xMax - 1f);
            float lineWidth = 4f; 
            Rect thresholdLineRect = new Rect(thresholdX - (lineWidth / 2f), barRect.y - 2f, lineWidth, barRect.height + 4f);
            GUI.color = isDraggingAnyThreshold ? DraggingColor : ThresholdLineColor;
            GUI.DrawTexture(thresholdLineRect, BaseContent.WhiteTex);
            GUI.color = Color.white;
            //重置状态
            Text.Anchor = TextAnchor.UpperLeft;
            //悬浮提示
            TooltipHandler.TipRegion(rect, $"{comp.Props.GalactogenUIDes}\n\n当前自动收集阈值: {comp.AutoGather * 100:F0}%");
            return new GizmoResult(GizmoState.Clear);
        }

        private float CalculatePulseAlpha()
        {
            float num = Mathf.Repeat(Time.time, TotalPulsateTime);
            if (num < 0.1f) return num / 0.1f;
            if (num < 0.25f) return 1f;
            return 1f - (num - 0.25f) / 0.6f;
        }

        private void HandleInteraction(Rect barRect)
        {
            Event current = Event.current;
            if (Mouse.IsOver(barRect) && current.type == EventType.MouseDown && current.button == 0)
            {
                isDraggingAnyThreshold = true;
                UpdateThreshold(current.mousePosition.x, barRect);
                current.Use();
            }
            if (isDraggingAnyThreshold)
            {
                if (current.type == EventType.MouseUp || !Input.GetMouseButton(0))
                {
                    isDraggingAnyThreshold = false;
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                    current.Use();
                }
                else
                {
                    UpdateThreshold(current.mousePosition.x, barRect);
                    if (current.type == EventType.MouseDrag) current.Use();
                }
            }
        }

        private void UpdateThreshold(float mouseX, Rect barRect)
        {
            float relativeX = Mathf.Clamp(mouseX - barRect.x, 0f, barRect.width);
            comp.AutoGather = relativeX / barRect.width;
        }
    }
}