using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //定义武器可储存乳源质浓浆的槽位容量、状态名称和可用特殊模式。
    public class CompProperties_GalactogenStorageWeapon : CompProperties
    {
        public int slotCapacity = 3;
        public float gelShotRange = 33f;
        public bool showGelShot = true;
        public string slotLabel = "粘胶弹浓浆槽";

        //初始化武器浓浆储存组件的运行类型。
        public CompProperties_GalactogenStorageWeapon()
        {
            compClass = typeof(Comp_GalactogenStorageWeapon);
        }
    }

    //保存武器内乳源质浓浆槽位，并为特殊弹药消耗与状态显示提供接口。
    public class Comp_GalactogenStorageWeapon : ThingComp
    {
        private int slotCount;
        private Ability_GalactogenGelShot cachedGelShotAbility;

        private CompProperties_GalactogenStorageWeapon Props
        {
            get { return (CompProperties_GalactogenStorageWeapon)props; }
        }

        public int SlotCount
        {
            get { return slotCount; }
        }

        public int SlotCapacity
        {
            get { return Props.slotCapacity; }
        }

        public bool SlotFull
        {
            get { return slotCount >= SlotCapacity; }
        }

        public string SlotLabel
        {
            get { return Props.slotLabel.NullOrEmpty() ? "武器浓浆槽" : Props.slotLabel; }
        }

        //生成当前武器浓浆槽状态与可用特殊能力按钮。
        public IEnumerable<Gizmo> GetStorageGizmos(Pawn pawn)
        {
            yield return new Gizmo_GalactogenWeaponSlotBar(this);
            if (Props.showGelShot)
            {
                Gizmo gelGizmo = GetGelShotAbility(pawn).GetGizmos().FirstOrDefault();
                if (gelGizmo != null)
                {
                    yield return gelGizmo;
                }
            }
        }

        //获取绑定当前持有者的化合粘胶弹能力实例，避免每帧重建能力与 VerbTracker。
        public Ability_GalactogenGelShot GetGelShotAbility(Pawn pawn)
        {
            if (pawn == null || cachedGelShotAbility?.pawn != pawn)
            {
                cachedGelShotAbility = new Ability_GalactogenGelShot(pawn, MunoDefRef.MunoDefDataRef.Muno_GalactogenGelShot);
            }

            return cachedGelShotAbility;
        }

        //向武器浓浆槽中加入指定数量浓浆，并返回实际加入数量。
        public int AddSlot(int amount)
        {
            int canAdd = SlotCapacity - slotCount;
            int actual = Mathf.Min(amount, canAdd);
            slotCount += actual;
            return actual;
        }

        //检查武器浓浆槽中是否有足够数量供能力消耗。
        public bool HasEnough(int amount)
        {
            return slotCount >= amount;
        }

        //消耗指定数量的武器浓浆槽，数量不足时返回失败。
        public bool ConsumeSlot(int amount)
        {
            if (slotCount < amount)
            {
                return false;
            }

            slotCount -= amount;
            return true;
        }

        //为武器特殊射击消耗浓浆槽，并在不足时给玩家提示。
        public bool TryConsumeForGelShot(Pawn pawn, int amount)
        {
            if (ConsumeSlot(amount))
            {
                return true;
            }

            Messages.Message("机炮浓浆槽不足，无法发射化合粘胶弹。", pawn, MessageTypeDefOf.RejectInput);
            return false;
        }

        //保存和读取武器内当前浓浆槽数量。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref slotCount, "slotCount", 0);
        }
    }

    //显示武器浓浆槽状态的分格 Gizmo。
    public class Gizmo_GalactogenWeaponSlotBar : Gizmo
    {
        private readonly Comp_GalactogenStorageWeapon comp;
        private static readonly Color ColorFull = new Color(0.35f, 0.82f, 1f);
        private static readonly Color ColorEmpty = new Color(0.2f, 0.2f, 0.2f);

        //绑定需要显示状态的武器浓浆储存组件。
        public Gizmo_GalactogenWeaponSlotBar(Comp_GalactogenStorageWeapon comp)
        {
            this.comp = comp;
            Order = -98f;
        }

        //返回 Gizmo 在命令栏中的固定宽度。
        public override float GetWidth(float maxWidth)
        {
            return 136f;
        }

        //绘制武器浓浆槽标题、当前数量与分格槽位。
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(outerRect);

            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            try
            {
                Rect labelRect = new Rect(outerRect.x + 4f, outerRect.y + 4f, outerRect.width - 8f, Text.LineHeightOf(GameFont.Tiny) + 4f);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(labelRect, comp.SlotLabel + "  " + comp.SlotCount + "/" + comp.SlotCapacity);

                float cellW = (outerRect.width - 12f) / comp.SlotCapacity;
                float cellH = 30f;
                float cellY = outerRect.y + 32f;
                for (int i = 0; i < comp.SlotCapacity; i++)
                {
                    Rect cell = new Rect(outerRect.x + 6f + i * cellW, cellY, cellW - 3f, cellH);
                    Widgets.DrawBoxSolid(cell, i < comp.SlotCount ? ColorFull : ColorEmpty);
                    Widgets.DrawBox(cell, 1);
                }
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                GUI.color = oldColor;
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }
}
