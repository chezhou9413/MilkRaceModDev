using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //计算激光炮锥形格、绘制瞄准预览，并对格内 Pawn 与建筑各施加一次热能伤害。
    public class CompAbilityEffect_MunoRadiationCone : CompAbilityEffect
    {
        private readonly List<IntVec3> affectedCells = new List<IntVec3>();
        private readonly HashSet<Thing> affectedThings = new HashSet<Thing>();

        //返回能力 Verb 上由 XML 配置的锥形辐射参数。
        private VerbProperties_MunoRadiationCone ConeProps
        {
            get { return parent.verb?.verbProps as VerbProperties_MunoRadiationCone; }
        }

        //校验目标、武器类型、XML 参数和武器槽加共享备用槽数量。
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            Pawn pawn = parent.pawn;
            ThingWithComps weapon = pawn?.equipment?.Primary;
            Comp_GalactogenStorageWeapon storage = weapon?.GetComp<Comp_GalactogenStorageWeapon>();
            if (weapon?.def != MunoDefDataRef.Gun_MunoLC || storage == null || ConeProps == null || ConeProps.damageDef == null)
            {
                if (throwMessages)
                {
                    Messages.Message("缪诺激光炮的辐射放射配置无效。", pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            if (!GalactogenStorageUtility.HasEnough(pawn, storage, ConeProps.storageCost))
            {
                if (throwMessages)
                {
                    Messages.Message("激光炮浓浆槽与储存罐均不足。", pawn, MessageTypeDefOf.RejectInput);
                }

                return false;
            }

            return target.Cell.IsValid && target.Cell != pawn.Position;
        }

        //以原版范围边线绘制当前 XML 射程和锥角计算出的全部有效格。
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            base.DrawEffectPreview(target);
            GenDraw.DrawFieldEdges(CalculateAffectedCells(target));
        }

        //暖机完成后扣除一次浓浆，再对锥形内所有有效 Pawn 与建筑进行去重伤害。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn pawn = parent.pawn;
            ThingWithComps weapon = pawn?.equipment?.Primary;
            Comp_GalactogenStorageWeapon storage = weapon?.GetComp<Comp_GalactogenStorageWeapon>();
            VerbProperties_MunoRadiationCone props = ConeProps;
            if (weapon?.def != MunoDefDataRef.Gun_MunoLC || storage == null || props?.damageDef == null)
            {
                Messages.Message("缪诺激光炮已被卸下，辐射放射已取消。", pawn, MessageTypeDefOf.RejectInput);
                return;
            }

            if (!GalactogenStorageUtility.TryConsume(pawn, storage, props.storageCost, "激光炮浓浆槽与储存罐均不足，辐射放射已取消。"))
            {
                return;
            }

            affectedThings.Clear();
            List<IntVec3> cells = CalculateAffectedCells(target);
            SpawnConfiguredEffects(cells, target, props);
            for (int i = 0; i < cells.Count; i++)
            {
                List<Thing> things = cells[i].GetThingList(pawn.Map);
                for (int j = 0; j < things.Count; j++)
                {
                    Thing thing = things[j];
                    if (thing != pawn && !thing.Destroyed && (thing is Pawn || thing is Building))
                    {
                        affectedThings.Add(thing);
                    }
                }
            }

            foreach (Thing thing in affectedThings)
            {
                float angle = (thing.Position - pawn.Position).AngleFlat;
                thing.TakeDamage(new DamageInfo(props.damageDef, props.damageAmount, props.armorPenetration, angle, pawn));
            }
        }

        //按 XML 配置在锥形区域散布光点，并在瞄准点生成一次集中闪光。
        private void SpawnConfiguredEffects(List<IntVec3> cells, LocalTargetInfo target, VerbProperties_MunoRadiationCone props)
        {
            Map map = parent.pawn.Map;
            if (props.coneFleckDef != null && props.coneFleckChancePerCell > 0f)
            {
                float chance = Mathf.Clamp01(props.coneFleckChancePerCell);
                for (int i = 0; i < cells.Count; i++)
                {
                    IntVec3 cell = cells[i];
                    if (Rand.Chance(chance) && cell.ShouldSpawnMotesAt(map))
                    {
                        Vector3 position = cell.ToVector3Shifted() + Gen.RandomHorizontalVector(0.32f);
                        FleckMaker.Static(position, map, props.coneFleckDef, props.coneFleckScale.RandomInRange);
                    }
                }
            }

            if (props.targetFleckDef != null && target.Cell.InBounds(map) && target.Cell.ShouldSpawnMotesAt(map))
            {
                FleckMaker.Static(target.Cell, map, props.targetFleckDef, Mathf.Max(0.01f, props.targetFleckScale));
            }
        }

        //根据 XML 射程与完整锥角筛选格子，并使用原版射线判定阻止伤害穿墙。
        private List<IntVec3> CalculateAffectedCells(LocalTargetInfo target)
        {
            affectedCells.Clear();
            Pawn pawn = parent.pawn;
            VerbProperties_MunoRadiationCone props = ConeProps;
            if (pawn?.Map == null || props == null || !target.Cell.IsValid || target.Cell == pawn.Position)
            {
                return affectedCells;
            }

            Vector3 origin = pawn.Position.ToVector3Shifted().Yto0();
            Vector3 direction = (target.Cell.ToVector3Shifted().Yto0() - origin).normalized;
            float halfAngle = Mathf.Clamp(props.coneAngleDegrees, 0.1f, 179f) * 0.5f;
            int cellCount = GenRadial.NumCellsInRadius(props.range);
            for (int i = 0; i < cellCount; i++)
            {
                IntVec3 cell = pawn.Position + GenRadial.RadialPattern[i];
                if (!CanUseCell(cell, props.range))
                {
                    continue;
                }

                Vector3 cellDirection = (cell.ToVector3Shifted().Yto0() - origin).normalized;
                if (Vector3.Angle(direction, cellDirection) <= halfAngle)
                {
                    affectedCells.Add(cell);
                }
            }

            return affectedCells;
        }

        //判断格子是否在地图、射程和当前武器视线范围内，并排除射手所在格。
        private bool CanUseCell(IntVec3 cell, float range)
        {
            Pawn pawn = parent.pawn;
            if (!cell.InBounds(pawn.Map) || cell == pawn.Position || !cell.InHorDistOf(pawn.Position, range))
            {
                return false;
            }

            ShootLine shootLine;
            return parent.verb.TryFindShootLineFromTo(pawn.Position, cell, out shootLine);
        }
    }
}
