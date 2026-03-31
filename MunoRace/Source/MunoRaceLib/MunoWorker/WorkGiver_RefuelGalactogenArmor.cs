using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoWorker
{
    public class WorkGiver_RefuelGalactogenArmor : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForGroup(ThingRequestGroup.Pawn); }
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.ClosestTouch; }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t != pawn)
            {
                return false;
            }

            Comp_GalactogenStorageArmor comp = GetArmorComp(pawn);
            if (comp == null || comp.SlotFull || pawn.Downed || pawn.Drafted)
            {
                return false;
            }

            return HasAvailableFuel(pawn);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Comp_GalactogenStorageArmor comp = GetArmorComp(pawn);
            if (comp == null)
            {
                return null;
            }

            Thing fuel = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(MunoDefDataRef.MunoRace_ConcentratedMulacte),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn),
                validator: x => !x.IsForbidden(pawn) && pawn.CanReserve(x)
            );
            if (fuel == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_RefuelGalactogenArmor, pawn, fuel);
            job.count = comp.SlotCapacity - comp.SlotCount;
            return job;
        }

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

        private bool HasAvailableFuel(Pawn pawn)
        {
            return pawn.Map.listerThings
                .ThingsOfDef(MunoDefDataRef.MunoRace_ConcentratedMulacte)
                .Any(x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
        }
    }
}
