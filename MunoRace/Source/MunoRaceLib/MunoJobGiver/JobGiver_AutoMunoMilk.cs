using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoJobGiver
{
    public class JobGiver_AutoMunoMilk : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            ThingComp_Galactogen galactogen = pawn.GetComp<ThingComp_Galactogen>();
            if (galactogen == null || pawn.Dead || pawn.Downed || pawn.Drafted)
            {
                return null;
            }
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition))
            {
                return null;
            }
            if (!galactogen.autoCollectEnabled)
            {
                return null;
            }
            if (galactogen.CurrentGalactogen < (galactogen.MaxGalactogen * galactogen.AutoGather))
            {
                return null; 
            }
            if(galactogen.CurrentGalactogen <= 0f)
            {
                return null;
            }
            // 检查当前是否已经在执行这个Job,避免冲突
            if (pawn.CurJobDef == MunoDefDataRef.JobDriver_SpawnMunoMilk)
            {
                return null;
            }
            // 返回Job 实例
            Job job = JobMaker.MakeJob(MunoDefDataRef.JobDriver_SpawnMunoMilk, pawn);
            job.count = 25;
            return job;
        }
    }
}
