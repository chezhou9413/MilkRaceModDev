using RimWorld;
using Verse;
using Verse.Sound;

namespace MunoRaceLib.MunoBiotech.Weapons
{
    //在液滴落点及相邻可见格施加涂层，保留喷射弹丸的原版飞行表现。
    public class Projectile_MunoCoating : Projectile
    {
        //命中护盾时由护盾吸收，正常落地时施加状态并播放泡沫反馈。
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (!blockedByShield)
            {
                HediffDef coating = def == MunoBiotechDefOf.Bullet_MunoAcidGel ? MunoBiotechDefOf.Muno_AcidGelCoating
                    : def == MunoBiotechDefOf.Bullet_MunoMedicalFoam ? MunoBiotechDefOf.Muno_MedicalFoamCoating
                    : MunoBiotechDefOf.Muno_StrongGelCoating;
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, 1.1f, true))
                {
                    if (!cell.InBounds(Map) || !GenSight.LineOfSight(Position, cell, Map, skipFirstCell: true))
                        continue;
                    foreach (Thing thing in cell.GetThingList(Map))
                    {
                        if (thing is Pawn pawn && !pawn.Dead)
                            ApplyCoating(pawn, coating);
                    }
                }
                if (def.projectile.filth != null && !Position.Filled(Map))
                    FilthMaker.TryMakeFilth(Position, Map, def.projectile.filth);
                def.projectile.soundImpact?.PlayOneShot(SoundInfo.InMap(this));
                Effecter effect = def.projectile.landedEffecter?.Spawn(Position, Map);
                effect?.Cleanup();
            }
            Destroy();
        }

        //同类涂层只刷新时长，不叠加属性惩罚或重置酸蚀增长时间。
        private static void ApplyCoating(Pawn pawn, HediffDef coating)
        {
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(coating);
            if (existing != null)
            {
                existing.TryGetComp<HediffComp_Disappears>().ResetElapsedTicks();
                return;
            }
            pawn.health.AddHediff(coating);
        }
    }
}
