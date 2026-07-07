using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //定义战略步枪榴弹仓的容量、装填材料和每发材料消耗。
    public class CompProperties_StrategicGrenadeStorageWeapon : CompProperties
    {
        public int ammoCapacity = 5;
        public int steelPerGrenade = 1;

        //绑定战略步枪榴弹仓组件类型。
        public CompProperties_StrategicGrenadeStorageWeapon()
        {
            compClass = typeof(Comp_StrategicGrenadeStorageWeapon);
        }
    }

    //保存战略步枪榴弹数量，并提供状态条与射击消耗接口。
    public class Comp_StrategicGrenadeStorageWeapon : ThingComp
    {
        private int ammoCount;
        private static readonly Color ColorFull = new Color(0.95f, 0.62f, 0.24f);
        private static readonly Color ColorEmpty = new Color(0.2f, 0.2f, 0.2f);

        private CompProperties_StrategicGrenadeStorageWeapon Props
        {
            get { return (CompProperties_StrategicGrenadeStorageWeapon)props; }
        }

        public int AmmoCount
        {
            get { return ammoCount; }
        }

        public int AmmoCapacity
        {
            get { return Mathf.Max(1, Props.ammoCapacity); }
        }

        public int SteelPerGrenade
        {
            get { return Mathf.Max(1, Props.steelPerGrenade); }
        }

        public bool AmmoFull
        {
            get { return ammoCount >= AmmoCapacity; }
        }

        //返回战略步枪榴弹仓相关 Gizmo。
        public IEnumerable<Gizmo> GetStorageGizmos(Pawn pawn)
        {
            yield return new Gizmo_StrategicGrenadeSlotBar(this);
        }

        //将指定数量榴弹写入榴弹仓，并返回实际装入数量。
        public int AddAmmo(int amount)
        {
            int actual = Mathf.Min(amount, AmmoCapacity - ammoCount);
            if (actual <= 0)
            {
                return 0;
            }

            ammoCount += actual;
            return actual;
        }

        //判断榴弹仓是否还能发射一发榴弹。
        public bool HasAmmo()
        {
            return ammoCount > 0;
        }

        //消耗一发榴弹，并在不足时给玩家提示。
        public bool TryConsumeGrenade(Pawn pawn)
        {
            if (ammoCount > 0)
            {
                ammoCount--;
                return true;
            }

            Messages.Message("战略步枪榴弹仓为空。", pawn, MessageTypeDefOf.RejectInput, false);
            return false;
        }

        //保存和读取榴弹仓当前弹量。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ammoCount, "munoStrategicGrenadeAmmo", 0);
        }

        //绘制战略榴弹仓标题、当前数量和分格槽位。
        public void DrawSlotBar(Rect outerRect)
        {
            Widgets.DrawWindowBackground(outerRect);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect labelRect = new Rect(outerRect.x + 4f, outerRect.y + 4f, outerRect.width - 8f, Text.LineHeightOf(GameFont.Tiny) + 4f);
                Widgets.Label(labelRect, "战略榴弹  " + ammoCount + "/" + AmmoCapacity);

                float cellW = (outerRect.width - 12f) / AmmoCapacity;
                float cellH = 30f;
                float cellY = outerRect.y + 32f;
                for (int i = 0; i < AmmoCapacity; i++)
                {
                    Rect cell = new Rect(outerRect.x + 6f + i * cellW, cellY, cellW - 3f, cellH);
                    Widgets.DrawBoxSolid(cell, i < ammoCount ? ColorFull : ColorEmpty);
                    Widgets.DrawBox(cell, 1);
                }
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
            }
        }
    }

    //在命令栏中显示战略步枪榴弹仓状态。
    public class Gizmo_StrategicGrenadeSlotBar : Gizmo
    {
        private readonly Comp_StrategicGrenadeStorageWeapon comp;

        //绑定需要显示的战略榴弹仓组件。
        public Gizmo_StrategicGrenadeSlotBar(Comp_StrategicGrenadeStorageWeapon comp)
        {
            this.comp = comp;
            Order = -97f;
        }

        //返回 Gizmo 固定宽度。
        public override float GetWidth(float maxWidth)
        {
            return 136f;
        }

        //绘制战略榴弹仓状态条。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            comp.DrawSlotBar(outerRect);
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
