using MunoRaceLib.MunoDefRef;
using MunoRaceLib.Tool;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MunoRaceLib.MunoComp
{
    /// <summary>
    /// 实现化合粘胶弹的无伤害爆炸、范围减速、灭火与四散特效。
    /// </summary>
    public class Projectile_GalactogenGel : Projectile_Explosive
    {
        private int ticksToDetonation;
        private int curveIndex;
        private float curveHeight = 1.6f;

        /// <summary>
        /// 设置本枚粘胶弹的曲线偏移参数，使三发弹道彼此分离。
        /// </summary>
        public void ConfigureCurve(int curveIndex, float curveHeight)
        {
            this.curveIndex = curveIndex;
            this.curveHeight = curveHeight;
        }

        /// <summary>
        /// 返回沿二次贝塞尔曲线移动的弹丸精确位置。
        /// </summary>
        public override Vector3 ExactPosition
        {
            get
            {
                float t = Mathf.Clamp01(1f - (float)ticksToImpact / StartingTicksToImpact);
                Vector3 start = origin;
                Vector3 end = destination;
                Vector3 side = Vector3.Cross((end - start).Yto0().normalized, Vector3.up);
                float travelDistance = (end - start).MagnitudeHorizontal();
                Vector3 control = (start + end) * 0.5f + Vector3.up * (curveHeight + travelDistance * 0.035f) + side * curveIndex * 1.8f;
                Vector3 first = Vector3.Lerp(start, control, t);
                Vector3 second = Vector3.Lerp(control, end, t);
                return Vector3.Lerp(first, second, t);
            }
        }

        /// <summary>
        /// 保存粘胶弹落地后的延迟引爆计时。
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksToDetonation, "ticksToDetonation", 0);
            Scribe_Values.Look(ref curveIndex, "curveIndex", 0);
            Scribe_Values.Look(ref curveHeight, "curveHeight", 1.6f);
        }

        /// <summary>
        /// 推进延迟引爆计时，到点后执行粘胶爆炸效果。
        /// </summary>
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            ThrowGelTrail();
            if (ticksToDetonation <= 0)
            {
                return;
            }

            ticksToDetonation -= delta;
            if (ticksToDetonation <= 0)
            {
                ExplodeGel();
            }
        }

        /// <summary>
        /// 在飞行途中生成白色粘液拖尾。
        /// </summary>
        private void ThrowGelTrail()
        {
            if (Map == null || landed || !ExactPosition.ShouldSpawnMotesAt(Map))
            {
                return;
            }

            float t = Mathf.Clamp01(1f - (float)ticksToImpact / StartingTicksToImpact);
            Vector3 trailPos = ExactPosition + Vector3Utility.FromAngleFlat(Rand.Range(0f, 360f)) * Rand.Range(0f, 0.18f);
            FleckCreationData data = FleckMaker.GetDataStatic(trailPos, Map, MunoDefDataRef.Muno_Fleck_MilkSplatter, Rand.Range(0.28f, 0.55f));
            data.instanceColor = new Color(1f, 1f, 1f, 0.9f);
            data.rotation = Rand.Range(0f, 360f);
            data.rotationRate = Rand.Range(-160f, 160f);
            data.velocityAngle = ExactRotation.eulerAngles.y + 180f + Rand.Range(-45f, 45f);
            data.velocitySpeed = Rand.Range(0.025f, 0.09f) + Mathf.Sin(t * Mathf.PI) * 0.035f;
            data.airTimeLeft = Rand.Range(18f, 32f);
            Map.flecks.CreateFleck(data);

            if (Rand.Chance(0.35f))
            {
                FleckCreationData spray = FleckMaker.GetDataStatic(trailPos, Map, MunoDefDataRef.Muno_Fleck_MilkSplatter, Rand.Range(0.16f, 0.3f));
                spray.instanceColor = new Color(0.86f, 0.96f, 1f, 0.75f);
                spray.rotation = Rand.Range(0f, 360f);
                spray.rotationRate = Rand.Range(-220f, 220f);
                spray.velocityAngle = ExactRotation.eulerAngles.y + 150f + Rand.Range(-70f, 70f);
                spray.velocitySpeed = Rand.Range(0.07f, 0.16f);
                spray.airTimeLeft = Rand.Range(10f, 20f);
                Map.flecks.CreateFleck(spray);
            }
        }

        /// <summary>
        /// 命中目标后按弹丸配置决定立即爆炸或短延迟爆炸。
        /// </summary>
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (blockedByShield || def.projectile.explosionDelay == 0)
            {
                ExplodeGel();
                return;
            }

            landed = true;
            ticksToDetonation = def.projectile.explosionDelay;
            GenExplosion.NotifyNearbyPawnsOfDangerousExplosive(this, DamageDefOf.Extinguish, launcher.Faction, launcher);
        }

        /// <summary>
        /// 在爆炸半径内执行灭火、敌人减速、粘胶飞溅和原版视觉爆炸。
        /// </summary>
        private void ExplodeGel()
        {
            Map map = Map;
            IntVec3 center = destination.ToIntVec3();
            if (map == null)
            {
                Destroy();
                return;
            }

            if (!center.IsValid || !center.InBounds(map))
            {
                center = Position;
            }
            float radius = def.projectile.explosionRadius;
            Thing instigator = launcher;

            Destroy();
            SpawnFirefoamExplosionVisual(center, map, radius, instigator);
            FilthGalactogenTool.SpawnMilkSplatter(center.ToVector3Shifted(), map, 14);
            SpawnFirefoamLikeImpact(center, map);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                ExtinguishFiresAt(cell, map, instigator);
                SpawnFirefoamFilthAt(cell, map);
            }

            SlowPawnsInRadius(center, map, radius);
        }

        /// <summary>
        /// 调用原版爆炸表现管线，只播放灭火泡沫式视觉和音效，不额外造成伤害或生成污物。
        /// </summary>
        private void SpawnFirefoamExplosionVisual(IntVec3 center, Map map, float radius, Thing instigator)
        {
            if (map == null)
            {
                return;
            }

            GenExplosion.DoExplosion(
                center,
                map,
                radius,
                DamageDefOf.Extinguish,
                instigator,
                0,
                0f,
                SoundDefOf.Explosion_FirefoamPopper,
                equipmentDef,
                def,
                intendedTarget.Thing,
                null,
                0f,
                1,
                null,
                null,
                255,
                applyDamageToExplosionCellsNeighbors: false,
                null,
                0f,
                1,
                0f,
                damageFalloff: false,
                origin.AngleToFlat(destination),
                null,
                null,
                doVisualEffects: true,
                DamageDefOf.Extinguish.expolosionPropagationSpeed,
                0f,
                doSoundEffects: true,
                null,
                1f);
        }

        /// <summary>
        /// 在粘胶弹落点播放原版灭火泡沫命中特效和泡沫喷出音效。
        /// </summary>
        private void SpawnFirefoamLikeImpact(IntVec3 center, Map map)
        {
            if (map == null)
            {
                return;
            }

            if (def.projectile.soundImpact != null)
            {
                SoundInfo soundInfo = SoundInfo.InMap(new TargetInfo(center, map));
                soundInfo.volumeFactor = 2f;
                def.projectile.soundImpact.PlayOneShot(soundInfo);
            }

            if (def.projectile.landedEffecter != null)
            {
                Effecter effecter = def.projectile.landedEffecter.Spawn(center, map);
                effecter.Cleanup();
            }
        }

        /// <summary>
        /// 按原版灭火泡沫弹逻辑在可放置格生成泡沫污物，阻止火焰复燃。
        /// </summary>
        private void SpawnFirefoamFilthAt(IntVec3 cell, Map map)
        {
            if (def.projectile.filth == null || cell.Filled(map))
            {
                return;
            }

            FilthMaker.TryMakeFilth(cell, map, def.projectile.filth, def.projectile.filthCount.RandomInRange);
        }

        /// <summary>
        /// 扑灭指定格子上的地面火焰和附着火焰。
        /// </summary>
        private void ExtinguishFiresAt(IntVec3 cell, Map map, Thing instigator)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = things.Count - 1; i >= 0; i--)
            {
                Fire fire = things[i] as Fire;
                if (fire != null && !fire.Destroyed)
                {
                    fire.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 9999f, 0f, -1f, instigator));
                }
            }
        }

        /// <summary>
        /// 给爆炸半径内的所有 Pawn 添加或刷新化合粘胶减速状态。
        /// </summary>
        private void SlowPawnsInRadius(IntVec3 center, Map map, float radius)
        {
            if (map == null)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || !pawn.Position.InHorDistOf(center, radius))
                {
                    continue;
                }

                Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(MunoDefDataRef.Muno_GelSlowdown);
                if (existing != null)
                {
                    pawn.health.RemoveHediff(existing);
                }

                pawn.health.AddHediff(MunoDefDataRef.Muno_GelSlowdown);
                MoteMaker.ThrowText(pawn.DrawPos, map, "粘胶附着", Color.cyan);
            }
        }

    }
}
