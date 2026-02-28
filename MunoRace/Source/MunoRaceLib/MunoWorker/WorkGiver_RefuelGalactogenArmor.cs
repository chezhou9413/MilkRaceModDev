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
        // 扫描地图上的所有 pawn
        public override ThingRequest PotentialWorkThingRequest
            => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // t 就是 pawn 自身
            if (t != pawn) return false;

            ThingComp_GalactogenArmor comp = GetArmorComp(pawn);
            if (comp == null || comp.SlotFull) return false;
            if (pawn.Downed || pawn.Drafted) return false;

            // 地图上有没有可用的浓浆
            return HasAvailableFuel(pawn);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            ThingComp_GalactogenArmor comp = GetArmorComp(pawn);
            if (comp == null) return null;

            Thing fuel = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForDef(MunoDefDataRef.MunoRace_ConcentratedMulacte),
                PathEndMode.ClosestTouch,
                TraverseParms.For(pawn),
                validator: x => !x.IsForbidden(pawn) && pawn.CanReserve(x)
            );
            if (fuel == null) return null;

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_RefuelGalactogenArmor, pawn, fuel);
            job.count = comp.SlotCapacity - comp.SlotCount;
            return job;
        }

        private ThingComp_GalactogenArmor GetArmorComp(Pawn pawn)
        {
            if (pawn?.apparel == null) return null;
            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                ThingComp_GalactogenArmor comp = ap.GetComp<ThingComp_GalactogenArmor>();
                if (comp != null) return comp;
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
