using System.Linq;
using MunoRaceLib.MunoScenarios;
using RimWorld;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoRecording
{
    //负责自动拍摄的对象筛选、取景条件检查和任务调度。
    public static class MunoCameraJobUtility
    {
        //判断摄影者是否能够安全执行拍摄动作。
        public static bool CanOperate(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.IsColonistPlayerControlled && !pawn.Downed
                && !pawn.Drafted && !pawn.InMentalState && pawn.Awake() && pawn.GetPosture() == PawnPosture.Standing
                && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
        }

        //判断对象是否是摄影者所在地图上的另一名正常活动殖民者。
        public static bool ValidSubject(Pawn wearer, Pawn subject)
        {
            return subject != null && subject != wearer && subject.Spawned && !subject.Dead && !subject.Downed
                && subject.Map == wearer.Map && subject.IsFreeColonist && !subject.IsQuestLodger()
                && !subject.InMentalState && subject.Awake();
        }

        //判断指定机位与对象之间是否满足距离和无遮挡取景条件。
        public static bool CanFrame(Pawn wearer, Pawn subject, IntVec3 cell)
        {
            return ValidSubject(wearer, subject) && cell.InBounds(wearer.Map)
                && cell.DistanceTo(subject.Position) <= MunoStoryConfigDef.Current.recordingRadius
                && cell != subject.Position && GenSight.LineOfSight(cell, subject.Position, wearer.Map);
        }

        //在不打断玩家命令、战斗、搬运和紧急需求的情况下自动发起拍摄。
        public static bool TryAutomatic(Pawn wearer, Comp_MunoCamera camera)
        {
            if (!CanOperate(wearer) || wearer.CurJob == null || wearer.CurJob.playerForced
                || !wearer.CurJob.def.casualInterruptible || wearer.CurJob.def == DefDatabase<JobDef>.GetNamed("Muno_RecordColonist")
                || wearer.stances.FullBodyBusy || wearer.carryTracker.CarriedThing != null
                || wearer.needs.food?.CurLevelPercentage < 0.3f || wearer.needs.rest?.CurLevelPercentage < 0.25f) return false;
            Pawn subject = wearer.Map.mapPawns.FreeColonistsSpawned
                .Where(p => CanFrame(wearer, p, wearer.Position)).InRandomOrder().FirstOrDefault();
            if (subject == null) return false;
            return TryStart(wearer, subject, camera);
        }

        //在当前位置启动自动拍摄任务，成功接单后设置摄像机冷却。
        private static bool TryStart(Pawn wearer, Pawn subject, Comp_MunoCamera camera)
        {
            if (!CanOperate(wearer) || !camera.CanStart || (camera.parent as Apparel)?.Wearer != wearer
                || !CanFrame(wearer, subject, wearer.Position)) return false;
            IntVec3 cell = wearer.Position;
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Muno_RecordColonist"), subject, camera.parent, cell);
            wearer.jobs.StartJob(job, JobCondition.InterruptOptional, resumeCurJobAfterwards: true,
                cancelBusyStances: false, preToilReservationsCanFail: true);
            if (wearer.CurJob != job) return false;
            camera.ScheduleNextCapture();
            return true;
        }
    }
}
