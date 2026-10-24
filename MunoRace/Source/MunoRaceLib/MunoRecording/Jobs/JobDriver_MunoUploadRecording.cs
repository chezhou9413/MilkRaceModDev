using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoRecording
{
    //预约上传站并在操作完成后重新核对、兑换录像带。
    public class JobDriver_MunoUploadRecording : JobDriver
    {
        //防止多名殖民者同时操作同一上传站。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        //到达交互位置后上传，移动中或操作中失去上传站会取消任务。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.Wait(120, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
            Toil upload = ToilMaker.MakeToil("缪诺上传录像带");
            upload.initAction = () => MunoRecordingExchange.Upload(pawn, (Building_MunoUploadStation)job.targetA.Thing, job.count);
            upload.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return upload;
        }
    }
}
