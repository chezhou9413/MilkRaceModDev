using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    public class ThingCompProperties_GalactogenArmor : CompProperties
    {
        public int jumpConcentratedCost = 1;
        public float jumpRange = 9.9f;
        public float jumpMinRange = 5.9f;
        public HediffDef motionBoostHediff;
        public string soundLandingDefName = "Longjump_Land";
        public int slotCapacity = 5;

        public ThingCompProperties_GalactogenArmor()
        {
            compClass = typeof(ThingComp_GalactogenArmor);
        }
    }

    public class ThingComp_GalactogenArmor : ThingComp
    {
        private int _slotCount = 0;

        public int SlotCount => _slotCount;
        public int SlotCapacity => Props.slotCapacity;
        public bool SlotFull => _slotCount >= Props.slotCapacity;

        public int AddSlot(int amount)
        {
            int canAdd = SlotCapacity - _slotCount;
            int actual = Mathf.Min(amount, canAdd);
            _slotCount += actual;
            return actual;
        }

        public bool ConsumeSlot(int amount)
        {
            if (_slotCount < amount) return false;
            _slotCount -= amount;
            return true;
        }

        public bool motionBoostEnabled = true;

        private ThingCompProperties_GalactogenArmor Props => (ThingCompProperties_GalactogenArmor)props;

        private SoundDef _landingSound;
        private SoundDef LandingSound
        {
            get
            {
                if (_landingSound == null && !Props.soundLandingDefName.NullOrEmpty())
                    _landingSound = DefDatabase<SoundDef>.GetNamed(Props.soundLandingDefName, errorOnFail: false);
                return _landingSound;
            }
        }
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (motionBoostEnabled)
                TryApplyMotionBoost(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveMotionBoost(pawn);
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60)) return;
            Pawn wearer = GetWearer();
            if (wearer == null) return;
            if (Props.motionBoostHediff != null)
            {
                bool hasHediff = wearer.health.hediffSet.GetFirstHediffOfDef(Props.motionBoostHediff) != null;
                if (motionBoostEnabled && !hasHediff)
                    TryApplyMotionBoost(wearer);
                else if (!motionBoostEnabled && hasHediff)
                    RemoveMotionBoost(wearer);
            }
        }
        public IEnumerable<Gizmo> GetArmorGizmos()
        {
            Pawn p = GetWearer();
            if (p == null) yield break;

            if (Props.motionBoostHediff != null)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "激活运动增强系统",
                    defaultDesc = "开启后提升移动速度 +10% 与近战闪避率 +10%",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true),
                    isActive = () => motionBoostEnabled,
                    toggleAction = () =>
                    {
                        motionBoostEnabled = !motionBoostEnabled;
                        if (motionBoostEnabled) TryApplyMotionBoost(p);
                        else RemoveMotionBoost(p);
                    }
                };
            }
            bool canJump = !p.Downed && _slotCount >= Props.jumpConcentratedCost;
            yield return new Command_Action
            {
                defaultLabel = "激活喷气系统",
                defaultDesc = $"消耗槽内 {Props.jumpConcentratedCost} 个乳原质浓浆进行远距离跳跃\n" +
                              $"（范围 {Props.jumpMinRange:F0}~{Props.jumpRange:F0} 格）\n" +
                              $"当前槽内浓浆：{_slotCount}/{SlotCapacity}",
                icon = ContentFinder<Texture2D>.Get("UI/Abilities/MechLongJump", true),
                Disabled = !canJump,
                disabledReason = p.Downed ? "小人已倒下" : "装甲浓浆槽已空",
                action = () => StartJumpTargeting(p)
            };

            yield return new Gizmo_ArmorSlotBar(this);
        }
        private void TryApplyMotionBoost(Pawn pawn)
        {
            if (Props.motionBoostHediff == null) return;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(Props.motionBoostHediff) != null) return;

            Hediff hediff = pawn.health.AddHediff(
                Props.motionBoostHediff,
                pawn.health.hediffSet.GetNotMissingParts()
                     .FirstOrFallback(r => r.def == BodyPartDefOf.Torso)
            );

            HediffComp_RemoveIfApparelDropped removeComp = hediff?.TryGetComp<HediffComp_RemoveIfApparelDropped>();
            if (removeComp != null)
                removeComp.wornApparel = (Apparel)parent;
        }

        private void RemoveMotionBoost(Pawn pawn)
        {
            if (Props.motionBoostHediff == null) return;
            Hediff h = pawn.health.hediffSet.GetFirstHediffOfDef(Props.motionBoostHediff);
            if (h != null)
                pawn.health.RemoveHediff(h);
        }

        private void StartJumpTargeting(Pawn pawn)
        {
            float range = Props.jumpRange;
            float minRange = Props.jumpMinRange;

            TargetingParameters tp = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetPawns = false,
                canTargetBuildings = false
            };

            Find.Targeter.BeginTargeting(
                tp,
                (LocalTargetInfo target) =>
                {
                    if (!ConsumeSlot(Props.jumpConcentratedCost))
                    {
                        Messages.Message("装甲浓浆槽已空，无法跳跃",
                            MessageTypeDefOf.RejectInput, historical: false);
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
                (LocalTargetInfo t) =>
                {
                    if (!t.Cell.IsValid || !t.Cell.InBounds(pawn.Map)) return false;
                    if (!JumpUtility.ValidJumpTarget(pawn, pawn.Map, t.Cell)) return false;
                    float distSq = (float)pawn.Position.DistanceToSquared(t.Cell);
                    return distSq >= minRange * minRange
                        && distSq <= range * range
                        && GenSight.LineOfSight(pawn.Position, t.Cell, pawn.Map);
                },
                pawn,
                null,
                null,
                true,
                null,
                (_) =>
                {
                    GenDraw.DrawRadiusRing(pawn.Position, range, Color.white);
                    GenDraw.DrawRadiusRing(pawn.Position, minRange, Color.red);
                }
            );
        }

        public Pawn GetWearer() => (parent as Apparel)?.Wearer;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref motionBoostEnabled, "motionBoostEnabled", true);
            Scribe_Values.Look(ref _slotCount, "slotCount", 0);
        }
    }
    public class Gizmo_ArmorSlotBar : Gizmo
    {
        private readonly ThingComp_GalactogenArmor _comp;

        private static readonly Color ColorFull = new Color(0.35f, 0.82f, 1f);
        private static readonly Color ColorEmpty = new Color(0.2f, 0.2f, 0.2f);

        public Gizmo_ArmorSlotBar(ThingComp_GalactogenArmor comp)
        {
            _comp = comp;
            Order = -99f;
        }

        public override float GetWidth(float maxWidth) => 136f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(outerRect);
            Rect labelRect = new Rect(outerRect.x + 4f, outerRect.y + 4f, outerRect.width - 8f, 20f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(labelRect, $"乳原质浓浆槽  {_comp.SlotCount}/{_comp.SlotCapacity}");
            float cellW = (outerRect.width - 12f) / _comp.SlotCapacity;
            float cellH = 30f;
            float cellY = outerRect.y + 28f;
            for (int i = 0; i < _comp.SlotCapacity; i++)
            {
                Rect cell = new Rect(outerRect.x + 6f + i * cellW, cellY, cellW - 3f, cellH);
                Widgets.DrawBoxSolid(cell, i < _comp.SlotCount ? ColorFull : ColorEmpty);
                Widgets.DrawBox(cell, 1);
            }
            Text.Font = GameFont.Small;
            return new GizmoResult(GizmoState.Clear);
        }
    }
}