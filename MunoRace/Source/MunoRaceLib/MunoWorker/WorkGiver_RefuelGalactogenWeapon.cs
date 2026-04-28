using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoWorker
{
    /// <summary>
    /// 为装备了可储存浓浆武器的小人寻找自动装填乳源质浓浆的搬运工作。
    /// </summary>
    public class WorkGiver_RefuelGalactogenWeapon : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get { return ThingRequest.ForGroup(ThingRequestGroup.Pawn); }
        }

        public override PathEndMode PathEndMode
        {
            get { return PathEndMode.ClosestTouch; }
        }

        /// <summary>
        /// 判断目标小人当前主武器是否需要装填，且地图上是否存在可用浓浆。
        /// </summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t != pawn || pawn.Downed || pawn.Drafted)
            {
                return false;
            }

            Comp_GalactogenStorageWeapon comp = GetWeaponComp(pawn);
            if (comp == null || comp.SlotFull)
            {
                return false;
            }

            return HasAvailableFuel(pawn);
        }

        /// <summary>
        /// 创建将最近可用浓浆装入当前主武器的工作。
        /// </summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Comp_GalactogenStorageWeapon comp = GetWeaponComp(pawn);
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

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDef_RefuelGalactogenWeapon, pawn, fuel);
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

        /// <summary>
        /// 检查地图中是否存在小人可预留、可到达的乳源质浓浆。
        /// </summary>
        private bool HasAvailableFuel(Pawn pawn)
        {
            return pawn.Map.listerThings
                .ThingsOfDef(MunoDefDataRef.MunoRace_ConcentratedMulacte)
                .Any(x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
        }
    }
}
