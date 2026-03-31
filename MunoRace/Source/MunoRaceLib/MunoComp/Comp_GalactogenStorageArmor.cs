using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    public interface IArmorGizmoProvider
    {
        IEnumerable<Gizmo> GetArmorGizmos(Pawn pawn, Comp_GalactogenStorageArmor storageComp);
    }

    public class CompProperties_GalactogenStorageArmor : CompProperties
    {
        public int slotCapacity = 5;
        public int jumpConcentratedCost = 0;
        public float jumpRange = 0f;
        public float jumpMinRange = 0f;
        public string soundLandingDefName = "Longjump_Land";

        public CompProperties_GalactogenStorageArmor()
        {
            compClass = typeof(Comp_GalactogenStorageArmor);
        }
    }

    public class Comp_GalactogenStorageArmor : ThingComp
    {
        private int slotCount;
        private SoundDef landingSound;

        private CompProperties_GalactogenStorageArmor Props
        {
            get { return (CompProperties_GalactogenStorageArmor)props; }
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

        private SoundDef LandingSound
        {
            get
            {
                if (landingSound == null && !Props.soundLandingDefName.NullOrEmpty())
                {
                    landingSound = DefDatabase<SoundDef>.GetNamed(Props.soundLandingDefName, false);
                }

                return landingSound;
            }
        }

        public int AddSlot(int amount)
        {
            int canAdd = SlotCapacity - slotCount;
            int actual = Mathf.Min(amount, canAdd);
            slotCount += actual;
            return actual;
        }

        public bool ConsumeSlot(int amount)
        {
            if (slotCount < amount)
            {
                return false;
            }

            slotCount -= amount;
            return true;
        }

        public bool HasEnough(int amount)
        {
            return slotCount >= amount;
        }

        public bool TryConsumeForAbility(Pawn pawn, int amount)
        {
            if (!ConsumeSlot(amount))
            {
                Messages.Message("装甲浓浆槽不足。", pawn, MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        public IEnumerable<Gizmo> GetStorageGizmos(Pawn pawn)
        {
            if (Props.jumpRange > 0f && Props.jumpConcentratedCost > 0)
            {
                bool canJump = !pawn.Downed && slotCount >= Props.jumpConcentratedCost;
                yield return new Command_Action
                {
                    defaultLabel = "激活喷气系统",
                    defaultDesc = "消耗槽内 " + Props.jumpConcentratedCost + " 个乳原质浓浆进行远距离跳跃\n（范围 "
                        + Props.jumpMinRange.ToString("F0") + "~" + Props.jumpRange.ToString("F0") + " 格）\n当前槽内浓浆："
                        + slotCount + "/" + SlotCapacity,
                    icon = ContentFinder<Texture2D>.Get("UI/Abilities/MechLongJump", true),
                    Disabled = !canJump,
                    disabledReason = pawn.Downed ? "小人已倒下" : "装甲浓浆槽已空",
                    action = delegate { StartJumpTargeting(pawn); }
                };
            }

            yield return new Gizmo_GalactogenArmorSlotBar(this);
        }

        private void StartJumpTargeting(Pawn pawn)
        {
            TargetingParameters targetParams = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = false
            };

            Find.Targeter.BeginTargeting(
                targetParams,
                delegate (LocalTargetInfo target)
                {
                    if (!ConsumeSlot(Props.jumpConcentratedCost))
                    {
                        Messages.Message("装甲浓浆槽已空，无法跳跃", MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    VerbProperties verbProps = new VerbProperties
                    {
                        verbClass = typeof(Verb_CastAbilityJump),
                        soundLanding = LandingSound,
                        flyWithCarriedThing = false
                    };
                    JumpUtility.DoJump(pawn, target, null, verbProps);
                },
                null,
                delegate (LocalTargetInfo target)
                {
                    if (!target.Cell.IsValid || !target.Cell.InBounds(pawn.Map))
                    {
                        return false;
                    }

                    if (!JumpUtility.ValidJumpTarget(pawn, pawn.Map, target.Cell))
                    {
                        return false;
                    }

                    float distSq = pawn.Position.DistanceToSquared(target.Cell);
                    return distSq >= Props.jumpMinRange * Props.jumpMinRange
                        && distSq <= Props.jumpRange * Props.jumpRange
                        && GenSight.LineOfSight(pawn.Position, target.Cell, pawn.Map);
                },
                pawn,
                null,
                null,
                true,
                null,
                delegate
                {
                    GenDraw.DrawRadiusRing(pawn.Position, Props.jumpRange, Color.white);
                    GenDraw.DrawRadiusRing(pawn.Position, Props.jumpMinRange, Color.red);
                }
            );
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref slotCount, "slotCount", 0);
        }
    }

    public class Gizmo_GalactogenArmorSlotBar : Gizmo
    {
        private readonly Comp_GalactogenStorageArmor comp;
        private static readonly Color ColorFull = new Color(0.35f, 0.82f, 1f);
        private static readonly Color ColorEmpty = new Color(0.2f, 0.2f, 0.2f);

        public Gizmo_GalactogenArmorSlotBar(Comp_GalactogenStorageArmor comp)
        {
            this.comp = comp;
            Order = -99f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 136f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(outerRect);
            Rect labelRect = new Rect(outerRect.x + 4f, outerRect.y + 4f, outerRect.width - 8f, 20f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(labelRect, "乳原质浓浆槽  " + comp.SlotCount + "/" + comp.SlotCapacity);

            float cellW = (outerRect.width - 12f) / comp.SlotCapacity;
            float cellH = 30f;
            float cellY = outerRect.y + 28f;
            for (int i = 0; i < comp.SlotCapacity; i++)
            {
                Rect cell = new Rect(outerRect.x + 6f + i * cellW, cellY, cellW - 3f, cellH);
                Widgets.DrawBoxSolid(cell, i < comp.SlotCount ? ColorFull : ColorEmpty);
                Widgets.DrawBox(cell, 1);
            }

            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
