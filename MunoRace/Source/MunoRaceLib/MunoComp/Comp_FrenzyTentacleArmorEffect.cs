using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class CompProperties_FrenzyTentacleArmorEffect : CompProperties
    {
        public const int ActiveBioLiningDurationTicks = 60000;

        public HediffDef passiveNerveHediff;
        public HediffDef activeBioLiningHediff;
        public HediffDef withdrawalHediff;
        public PawnKindDef minionPawnKind;
        public int minionSpawnCount = 4;
        public int dependencyTicks = 180000;
        public int passiveRepairPerHour = 8;

        public CompProperties_FrenzyTentacleArmorEffect()
        {
            compClass = typeof(Comp_FrenzyTentacleArmorEffect);
        }
    }

    public class Comp_FrenzyTentacleArmorEffect : ThingComp, IArmorGizmoProvider
    {
        private CompProperties_FrenzyTentacleArmorEffect Props
        {
            get { return (CompProperties_FrenzyTentacleArmorEffect)props; }
        }

        private Comp_TentacleArmorData Data
        {
            get { return parent.TryGetComp<Comp_TentacleArmorData>(); }
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            EnsureBoundHediff(pawn, Props.passiveNerveHediff, true);
            RemoveWithdrawal(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveBoundHediff(pawn, Props.passiveNerveHediff);
            RemoveBoundHediff(pawn, Props.activeBioLiningHediff);

            if (Data != null && Data.dependencyTriggered)
            {
                EnsureBoundHediff(pawn, Props.withdrawalHediff, false);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60))
            {
                return;
            }

            Pawn wearer = (parent as Apparel)?.Wearer;
            if (wearer == null)
            {
                return;
            }

            EnsureBoundHediff(wearer, Props.passiveNerveHediff, true);
            NormalizeActiveBioLining(wearer);
            HandleDependency(wearer);

            if (parent.IsHashIntervalTick(2500))
            {
                RepairDurability();
            }
        }

        public IEnumerable<Gizmo> GetArmorGizmos(Pawn pawn, Comp_GalactogenStorageArmor storageComp)
        {
            yield return new Command_Action
            {
                defaultLabel = "分化次级个体",
                defaultDesc = "消耗槽内 1 个乳源质浓浆，生成 " + Props.minionSpawnCount + " 个触手个体协助作战。",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                Disabled = storageComp == null || !storageComp.HasEnough(1) || pawn.Downed,
                disabledReason = pawn.Downed ? "小人已倒下" : (storageComp != null && storageComp.HasEnough(1) ? string.Empty : "装甲浓浆槽不足"),
                action = delegate
                {
                    if (storageComp != null && storageComp.TryConsumeForAbility(pawn, 1))
                    {
                        SpawnMinions(pawn);
                    }
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "激活生物内衬(狂乱)",
                defaultDesc = "主动技能：消耗槽内 1 个乳源质浓浆，4 小时内提升利器/钝器防护各 40%，心情 +18，意识 +15%。",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", true),
                Disabled = storageComp == null || !storageComp.HasEnough(1) || pawn.Downed,
                disabledReason = pawn.Downed ? "小人已倒下" : (storageComp != null && storageComp.HasEnough(1) ? string.Empty : "装甲浓浆槽不足"),
                action = delegate
                {
                    if (storageComp != null && storageComp.TryConsumeForAbility(pawn, 1))
                    {
                        ActivateTimedHediff(pawn, Props.activeBioLiningHediff, CompProperties_FrenzyTentacleArmorEffect.ActiveBioLiningDurationTicks);
                    }
                }
            };

        }

        private void HandleDependency(Pawn wearer)
        {
            if (Data == null)
            {
                return;
            }

            Data.wornTicks += 60;
            if (!Data.dependencyTriggered && Data.wornTicks >= Props.dependencyTicks)
            {
                Data.dependencyTriggered = true;
                Messages.Message(wearer.LabelShort + " 已对失控触手甲产生强制依赖。", wearer, MessageTypeDefOf.NegativeEvent);
            }
        }

        private void ActivateTimedHediff(Pawn pawn, HediffDef hediffDef, int durationTicks)
        {
            Hediff hediff = EnsureBoundHediff(pawn, hediffDef, false);
            if (hediff == null)
            {
                return;
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = durationTicks;
            }
        }

        private void NormalizeActiveBioLining(Pawn pawn)
        {
            if (Props.activeBioLiningHediff == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.activeBioLiningHediff);
            if (hediff == null)
            {
                return;
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears == null)
            {
                pawn.health.RemoveHediff(hediff);
                return;
            }

            if (disappears.ticksToDisappear > CompProperties_FrenzyTentacleArmorEffect.ActiveBioLiningDurationTicks)
            {
                disappears.ticksToDisappear = CompProperties_FrenzyTentacleArmorEffect.ActiveBioLiningDurationTicks;
            }
        }

        private void SpawnMinions(Pawn pawn)
        {
            for (int i = 0; i < Props.minionSpawnCount; i++)
            {
                PawnKindDef kindDef = Props.minionPawnKind ?? MunoDefDataRef.Muno_Mech_Tentacles;
                Pawn generated = PawnGenerator.GeneratePawn(kindDef, Faction.OfPlayer ?? pawn.Faction);
                if (Faction.OfPlayer != null && generated.Faction != Faction.OfPlayer)
                {
                    generated.SetFaction(Faction.OfPlayer);
                }

                FleshbeastUtility.SpawnPawnAsFlyer(generated, pawn.Map, pawn.Position, 7, false);
                FleckMaker.ThrowDustPuff(pawn.DrawPos, pawn.Map, 1.2f);

                if (MunoDefDataRef.Muno_TentacleMinionLifetime != null)
                {
                    Hediff lifetime = generated.health.AddHediff(MunoDefDataRef.Muno_TentacleMinionLifetime);
                    HediffComp_Disappears disappears = lifetime?.TryGetComp<HediffComp_Disappears>();
                    if (disappears != null)
                    {
                        disappears.ticksToDisappear = 90000;
                    }
                }
            }
        }

        private void RepairDurability()
        {
            if (parent.HitPoints < parent.MaxHitPoints)
            {
                parent.HitPoints = Mathf.Min(parent.MaxHitPoints, parent.HitPoints + Props.passiveRepairPerHour);
            }
        }

        private void RemoveWithdrawal(Pawn pawn)
        {
            if (Props.withdrawalHediff == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.withdrawalHediff);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        private Hediff EnsureBoundHediff(Pawn pawn, HediffDef hediffDef, bool bindToApparel)
        {
            if (hediffDef == null)
            {
                return null;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = pawn.health.AddHediff(hediffDef, pawn.health.hediffSet.GetNotMissingParts().FirstOrFallback(r => r.def == BodyPartDefOf.Torso));
            }

            if (bindToApparel)
            {
                HediffComp_RemoveIfApparelDropped removeComp = hediff?.TryGetComp<HediffComp_RemoveIfApparelDropped>();
                if (removeComp != null)
                {
                    removeComp.wornApparel = (Apparel)parent;
                }
            }

            return hediff;
        }

        private void RemoveBoundHediff(Pawn pawn, HediffDef hediffDef)
        {
            if (hediffDef == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}
