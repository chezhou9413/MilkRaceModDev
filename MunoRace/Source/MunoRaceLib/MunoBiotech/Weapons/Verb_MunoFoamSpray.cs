using MunoRaceLib.MunoComp;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoBiotech.Weapons
{
    //复用灭火炮塔的弧形喷射，在每轮开始时消耗浓浆并固定弹丸模式。
    public class Verb_MunoFoamSpray : Verb_ArcSpray
    {
        private ThingDef burstProjectile;

        //一轮中允许耗尽最后一份浓浆后继续喷射。
        public override bool Available()
        {
            Comp_GalactogenStorageWeapon storage = EquipmentSource?.GetComp<Comp_GalactogenStorageWeapon>();
            return base.Available() && storage != null
                && (state == VerbState.Bursting || GalactogenStorageUtility.HasEnough(CasterPawn, storage, 1));
        }

        //只在预热结束时扣除一次浓浆，后续液滴不再重复扣费。
        public override void WarmupComplete()
        {
            Comp_GalactogenStorageWeapon storage = EquipmentSource.GetComp<Comp_GalactogenStorageWeapon>();
            if (!GalactogenStorageUtility.TryConsume(CasterPawn, storage, 1, "发射器浓浆不足。"))
            {
                CasterPawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                return;
            }
            burstProjectile = EquipmentSource.GetComp<Comp_MunoMultilauncher>().SelectedProjectile;
            base.WarmupComplete();
        }

        //原版散布允许重复落点，用中心落点补齐未生成的喷射路径。
        protected override void PreparePath()
        {
            base.PreparePath();
            while (path.Count < ShotsPerBurst)
                path.Add(currentTarget.Cell);
        }

        //向实际可见的落点发射液滴，墙后的格子不会被喷射穿透。
        protected override void HitCell(IntVec3 cell)
        {
            Map map = caster.Map;
            if (!cell.InBounds(map) || !caster.Position.InHorDistOf(cell, verbProps.range)
                || !GenSight.LineOfSight(caster.Position, cell, map, skipFirstCell: true))
                return;
            base.HitCell(cell);
            Projectile projectile = (Projectile)GenSpawn.Spawn(burstProjectile, caster.Position, map);
            projectile.Launch(caster, caster.DrawPos, cell, currentTarget,
                ProjectileHitFlags.IntendedTarget, false, EquipmentSource);
        }

        //保存本轮已经锁定的弹丸类型，读档后继续原来的喷射。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref burstProjectile, "burstProjectile");
        }
    }
}
