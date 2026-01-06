using MunoRaceLib.MunoComp;
using System;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace MunoRaceLib.MunoGizmo
{
    public class Gizmo_GalactogenBar : Gizmo
    {
        public Pawn pawn;
        public ThingComp_Galactogen comp;
        private static readonly Color MilkyWhite = new Color(0.95f, 0.95f, 0.9f);
        private static readonly Color ProgressBarBg = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color TextGreenColor = new Color(0f, 1f, 0f);
        private static readonly Color KnobNormalColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color KnobDraggingColor = new Color(0.3f, 0.6f, 1f);
        private static bool isDraggingAnyThreshold = false;

        public Gizmo_GalactogenBar(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public override float GetWidth(float maxWidth) => 212f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (comp == null) comp = pawn.GetComp<ThingComp_Galactogen>();
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);
            Rect contentRect = rect.ContractedBy(10f);
            Rect barRect = new Rect(contentRect.x, contentRect.y + 10f, contentRect.width, 32f);
            Widgets.DrawBoxSolid(barRect, ProgressBarBg);
            float fillPct = comp.CurrentGalactogen / Math.Max(1f, comp.MaxGalactogen);
            Widgets.FillableBar(barRect, fillPct, SolidColorMaterials.NewSolidColorTexture(MilkyWhite), BaseContent.ClearTex, false);
            HandleInteraction(barRect);
            float thresholdX = barRect.x + (barRect.width * comp.AutoGather);
            Rect lineRect = new Rect(thresholdX - 1f, barRect.y - 2f, 2f, barRect.height + 4f);
            Widgets.DrawBoxSolid(lineRect, isDraggingAnyThreshold ? KnobDraggingColor : KnobNormalColor);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = TextGreenColor;
            string label = $"{comp.Props.GalactogenUIName}: {comp.CurrentGalactogen:F0} / {comp.MaxGalactogen:F0}";
            Widgets.Label(barRect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, $"{comp.Props.GalactogenUIDes}\n\n当前自动收集阈值: {comp.AutoGather * 100:F0}%");
            return new GizmoResult(GizmoState.Clear);
        }

        private void HandleInteraction(Rect barRect)
        {
            Event current = Event.current;
            bool mouseOver = Mouse.IsOver(barRect);
            if (mouseOver && current.type == EventType.MouseDown && current.button == 0)
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
                    if (current.type == EventType.MouseDrag)
                    {
                        current.Use();
                    }
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