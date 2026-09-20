using MunoRaceLib.MunoDefRef;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoComp
{
    //统一装备自动汲取的数量档位、资源换算和任务调度规则。
    public static class GalactogenExtractionUtility
    {
        private static readonly int[] MilkAmounts = { 5, 25, 50, 100 };
        private static readonly int[] SlurryAmounts = { 1, 2, 5 };

        //返回当前产物模式的单次汲取数量。
        public static int SelectedAmount(ThingComp_Galactogen comp)
        {
            return comp.autoCollectMode == GalactogenAutoCollectMode.Milk
                ? comp.autoMilkAmount : comp.autoSlurryAmount;
        }

        //循环切换当前模式的数量，另一种产物的设置保持独立。
        public static void CycleAmount(ThingComp_Galactogen comp)
        {
            bool milk = comp.autoCollectMode == GalactogenAutoCollectMode.Milk;
            int[] amounts = milk ? MilkAmounts : SlurryAmounts;
            int index = System.Array.IndexOf(amounts, SelectedAmount(comp));
            int amount = amounts[(index + 1) % amounts.Length];
            if (milk) comp.autoMilkAmount = amount;
            else comp.autoSlurryAmount = amount;
        }

        //按当前可用资源计算整份产量，不把启动阈值当作保留资源。
        public static int AffordableAmount(ThingComp_Galactogen comp, GalactogenAutoCollectMode mode)
        {
            float unitCost = mode == GalactogenAutoCollectMode.Milk ? 1f : 100f;
            return Mathf.FloorToInt(Mathf.Max(0f, comp.CurrentGalactogen - comp.MinGalactogen) / unitCost);
        }

        //判断人物和装备是否允许自动汲取，用于启动及执行期间的状态检查。
        public static bool CanOperate(Pawn pawn, ThingComp_Galactogen comp)
        {
            return comp != null && comp.autoCollectEnabled && pawn.Spawned && pawn.IsColonistPlayerControlled
                && !pawn.Dead && !pawn.Downed && !pawn.Drafted && !pawn.InMentalState && pawn.Awake()
                && pawn.GetPosture() == PawnPosture.Standing
                && !pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition)
                && GalactogenExtractorUtility.HasActiveExtractor(pawn);
        }

        //达到触发阈值且至少可产一份时，锁定本次产物模式与数量。
        public static Job TryCreateJob(Pawn pawn, ThingComp_Galactogen comp)
        {
            if (!CanOperate(pawn, comp) || ThingComp_Galactogen.IsCollectionJob(pawn.CurJobDef)
                || comp.CurrentGalactogen < comp.MaxGalactogen * comp.AutoGather
                || pawn.needs.food?.CurLevelPercentage < 0.3f || pawn.needs.rest?.CurLevelPercentage < 0.25f)
                return null;

            int count = Mathf.Min(SelectedAmount(comp), AffordableAmount(comp, comp.autoCollectMode));
            if (count <= 0) return null;
            JobDef jobDef = comp.autoCollectMode == GalactogenAutoCollectMode.Milk
                ? MunoDefDataRef.JobDef_AutoExtractMunoMilk : MunoDefDataRef.JobDef_AutoExtractConcentratedMulacte;
            Job job = JobMaker.MakeJob(jobDef, pawn);
            job.count = count;
            return job;
        }

        //主动检查到期汲取，只暂停可中断的日常工作并保留可恢复任务。
        public static void TryStartPeriodic(ThingComp_Galactogen comp)
        {
            Pawn pawn = comp.SelfPawn;
            if (!CanOperate(pawn, comp) || pawn.CurJob == null || pawn.CurJob.playerForced
                || !pawn.CurJob.def.casualInterruptible || !pawn.jobs.IsCurrentJobPlayerInterruptible()
                || pawn.stances.FullBodyBusy || pawn.carryTracker.CarriedThing != null
                || pawn.mindState.enemyTarget != null) return;

            Job job = TryCreateJob(pawn, comp);
            if (job == null) return;
            pawn.jobs.StartJob(job, JobCondition.InterruptOptional, resumeCurJobAfterwards: true,
                cancelBusyStances: false, preToilReservationsCanFail: true);
        }
    }
}
