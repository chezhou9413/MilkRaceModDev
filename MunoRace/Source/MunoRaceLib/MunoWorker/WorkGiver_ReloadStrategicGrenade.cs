using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoWorker
{
    //为装备缪诺战略步枪的小人寻找钢铁，并支持自动或右键强制装填战略榴弹。
    public class WorkGiver_ReloadStrategicGrenade : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForDef(ThingDefOf.Steel); }
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.ClosestTouch; }
        }

        //判断目标钢铁是否能被当前小人用于装填战略步枪榴弹仓。
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t == null || t.def != ThingDefOf.Steel || pawn.Downed || (!forced && pawn.Drafted))
            {
                return false;
            }

            Comp_StrategicGrenadeStorageWeapon comp = GetWeaponComp(pawn);
            if (comp == null || comp.AmmoFull)
            {
                return false;
            }

            return !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly);
        }

        //创建将指定钢铁装入当前战略步枪榴弹仓的工作。
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Comp_StrategicGrenadeStorageWeapon comp = GetWeaponComp(pawn);
            if (comp == null || t == null || t.def != ThingDefOf.Steel)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_ReloadStrategicGrenade, pawn, t);
            job.count = (comp.AmmoCapacity - comp.AmmoCount) * comp.SteelPerGrenade;
            return job;
        }

        //获取小人当前主武器上的战略榴弹仓组件。
        private Comp_StrategicGrenadeStorageWeapon GetWeaponComp(Pawn pawn)
        {
            ThingWithComps primary = pawn?.equipment?.Primary;
            return primary?.GetComp<Comp_StrategicGrenadeStorageWeapon>();
        }
    }
}
