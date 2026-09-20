using System.Collections.Generic;
using MunoRaceLib.MunoScenarios;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoRecording
{
    //执行就位、举机、持续取景和收机动作，仅完整拍摄结束后写入片段。
    public class JobDriver_MunoRecordColonist : JobDriver
    {
        private int filmingTicks;
        private int duration;
        private bool filming;
        private bool committed;

        //返回本次使用的摄像机组件。
        private Comp_MunoCamera Camera => job.GetTarget(TargetIndex.B).Thing?.TryGetComp<Comp_MunoCamera>();

        //返回本次跟随取景的殖民者。
        private Pawn Subject => job.GetTarget(TargetIndex.A).Pawn;

        //向渲染器提供拍摄动作的归一化进度。
        public float Progress => duration > 0 ? Mathf.Clamp01((float)filmingTicks / duration) : 0f;

        //返回是否正在执行举机、取景或收机动作。
        public bool Filming => filming && !ended;

        //仅预留摄影机位，不锁定被拍摄者，允许其继续正常工作。
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.C), job, 1, -1, null, errorOnFailed);
        }

        //构建拍摄任务，状态失效或视野丢失时直接中断且不产出片段。
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => !MunoCameraJobUtility.CanOperate(pawn) || Camera == null
                || (Camera.parent as Apparel)?.Wearer != pawn || !MunoCameraJobUtility.ValidSubject(pawn, Subject));
            yield return Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.OnCell);
            Toil record = ToilMaker.MakeToil("缪诺摄像机取景");
            record.initAction = BeginFilming;
            record.tickIntervalAction = delta =>
            {
                if (!MunoCameraJobUtility.CanFrame(pawn, Subject, pawn.Position))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                pawn.rotationTracker.FaceTarget(Subject);
                int previousTicks = filmingTicks;
                filmingTicks += delta;
                if (filmingTicks / 45 != previousTicks / 45 && Progress > 0.2f && Progress < 0.8f)
                    MunoCameraEffects.RecordingPulse(pawn);
                if (previousTicks < duration * 0.8f && filmingTicks >= duration * 0.8f)
                    MunoCameraEffects.Shutter(pawn, Subject);
                if (filmingTicks >= duration) ReadyForNextToil();
            };
            record.handlingFacing = true;
            record.defaultCompleteMode = ToilCompleteMode.Never;
            record.socialMode = RandomSocialMode.Off;
            record.WithProgressBar(TargetIndex.A, () => Progress, alwaysShow: true);
            record.AddFinishAction(FinishFilming);
            yield return record;
            yield return Toils_General.Do(CommitClip);
        }

        //开始举机动作并清除静态人物图像缓存。
        private void BeginFilming()
        {
            duration = MunoStoryConfigDef.Current.recordingDurationTicks;
            filmingTicks = 0;
            filming = true;
            pawn.pather.StopDead();
            pawn.rotationTracker.FaceTarget(Subject);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        //在完成或中断时停止拍摄表现，恢复摄像机佩戴姿态。
        private void FinishFilming()
        {
            filming = false;
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        //完整拍摄结束后提交一次片段，并播放完成反馈。
        private void CommitClip()
        {
            if (committed || filmingTicks < duration || !MunoCameraJobUtility.CanFrame(pawn, Subject, pawn.Position)) return;
            committed = true;
            Camera.RecordClip(pawn);
            MunoCameraEffects.Finished(Subject);
        }

        //保存拍摄阶段、时长和提交标记，读档后继续同一段拍摄。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref filmingTicks, "filmingTicks");
            Scribe_Values.Look(ref duration, "duration");
            Scribe_Values.Look(ref filming, "filming");
            Scribe_Values.Look(ref committed, "committed");
        }
    }
}
