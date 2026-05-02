using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoWorker
{
    /// <summary>
    /// 为装备了可储存浓浆武器的小人寻找可用浓浆，并支持自动或右键强制装填。
    /// </summary>
    public class WorkGiver_RefuelGalactogenWeapon : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForDef(MunoDefDataRef.MunoRace_ConcentratedMulacte); }
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.ClosestTouch; }
        }

        /// <summary>
        /// 判断目标浓浆是否能被当前小人用于装填主武器。
        /// </summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t == null || t.def != MunoDefDataRef.MunoRace_ConcentratedMulacte || pawn.Downed || (!forced && pawn.Drafted))
            {
                return false;
            }

            Comp_GalactogenStorageWeapon comp = GetWeaponComp(pawn);
            if (comp == null || comp.SlotFull)
            {
                return false;
            }

            return !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly);
        }

        /// <summary>
        /// 创建将指定浓浆装入当前主武器的工作。
        /// </summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Comp_GalactogenStorageWeapon comp = GetWeaponComp(pawn);
            if (comp == null || t == null || t.def != MunoDefDataRef.MunoRace_ConcentratedMulacte)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_RefuelGalactogenWeapon, pawn, t);
            job.count = comp.SlotCapacity - comp.SlotCount;
            return job;
        }

        /// <summary>
        /// 获取小人当前主武器上的浓浆储存组件。
        /// </summary>
        private Comp_GalactogenStorageWeapon GetWeaponComp(Pawn pawn)
        {
            ThingWithComps primary = pawn?.equipment?.Primary;
            return primary?.GetComp<Comp_GalactogenStorageWeapon>();
        }

    }
}
