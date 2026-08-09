using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobGiver
{
    //根据乳源质阈值、穿戴装备与汲取模式生成对应的自动收集工作。
    public class JobGiver_AutoMunoMilk : ThinkNode_JobGiver
    {
        //检查小人状态并返回传统自动挤奶或装备自动汲取工作。
        protected override Job TryGiveJob(Pawn pawn)
        {
            ThingComp_Galactogen galactogen = pawn.GetComp<ThingComp_Galactogen>();
            if (!CanStartCollection(pawn, galactogen) || ThingComp_Galactogen.IsCollectionJob(pawn.CurJobDef))
            {
                return null;
            }

            float targetAmount = galactogen.MaxGalactogen * galactogen.AutoGather;
            if (!GalactogenExtractorUtility.HasActiveExtractor(pawn))
            {
                return BuildLegacyMilkJob(pawn, galactogen, targetAmount);
            }

            float surplus = Mathf.Max(0f, galactogen.CurrentGalactogen - targetAmount);
            if (galactogen.autoCollectMode == GalactogenAutoCollectMode.Milk)
            {
                int count = Mathf.FloorToInt(surplus);
                return count > 0 ? BuildExtractionJob(MunoDefDataRef.JobDef_AutoExtractMunoMilk, pawn, count) : null;
            }

            int slurryCount = Mathf.FloorToInt(surplus / 100f);
            return slurryCount > 0 ? BuildExtractionJob(MunoDefDataRef.JobDef_AutoExtractConcentratedMulacte, pawn, slurryCount) : null;
        }

        //检查自动收集所需的小人、营养和组件状态。
        private static bool CanStartCollection(Pawn pawn, ThingComp_Galactogen galactogen)
        {
            return galactogen != null
                && galactogen.autoCollectEnabled
                && !pawn.Dead
                && !pawn.Downed
                && !pawn.Drafted
                && !pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition);
        }

        //保留未穿戴汲取装备时达到阈值便收集二十五份缪诺乳的原有行为。
        private static Job BuildLegacyMilkJob(Pawn pawn, ThingComp_Galactogen galactogen, float targetAmount)
        {
            if (galactogen.CurrentGalactogen < targetAmount || galactogen.CurrentGalactogen <= 0f)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDriver_SpawnMunoMilk, pawn);
            job.count = 25;
            return job;
        }

        //创建指定产物模式和精确产量的装备自动汲取工作。
        private static Job BuildExtractionJob(JobDef jobDef, Pawn pawn, int count)
        {
            Job job = JobMaker.MakeJob(jobDef, pawn);
            job.count = count;
            return job;
        }
    }
}
