using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoWorker
{
    //为穿戴了可储存浓浆装甲的小人寻找浓浆，并支持自动和右键强制装填。
    public class WorkGiver_RefuelGalactogenArmor : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForDef(MunoDefDataRef.MunoRace_ConcentratedMulacte); }
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.ClosestTouch; }
        }

        //判断目标浓浆是否能被当前小人用于装填身上的装甲。
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t == null || t.def != MunoDefDataRef.MunoRace_ConcentratedMulacte || pawn.Downed || (!forced && pawn.Drafted))
            {
                return false;
            }

            Comp_GalactogenStorageArmor comp = GetArmorComp(pawn);
            if (comp == null || comp.SlotFull)
            {
                return false;
            }

            return !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.ClosestTouch, Danger.Deadly);
        }

        //创建将指定浓浆装入当前穿戴装甲的工作。
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Comp_GalactogenStorageArmor comp = GetArmorComp(pawn);
            if (comp == null || comp.SlotFull || t == null || t.def != MunoDefDataRef.MunoRace_ConcentratedMulacte)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_RefuelGalactogenArmor, pawn, t);
            job.count = comp.SlotCapacity - comp.SlotCount;
            return job;
        }

        //获取小人当前穿戴列表中第一个带浓浆储存组件的装甲。
        private Comp_GalactogenStorageArmor GetArmorComp(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return null;
            }

            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                Comp_GalactogenStorageArmor comp = ap.GetComp<Comp_GalactogenStorageArmor>();
                if (comp != null)
                {
                    return comp;
                }
            }

            return null;
        }
    }
}
