using System.Collections.Generic;
using MunoRaceLib.MunoScenarios;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoRecording
{
    //保存摄像机状态，定期请求拍摄任务并将完整片段写入实体录像带。
    public class Comp_MunoCamera : ThingComp
    {
        private int clips;
        private int nextCaptureTick;
        //返回摄像机是否已经冷却完成，可以发起下一段拍摄。
        public bool CanStart => Find.TickManager.TicksGame >= nextCaptureTick;

        //在摄像间隔到期后请求自动任务，不直接增加录像进度。
        public void Sample(Pawn wearer)
        {
            if (CanStart) MunoCameraJobUtility.TryAutomatic(wearer, this);
        }

        //设置下一次自动拍摄的随机时刻。
        public void ScheduleNextCapture()
        {
            nextCaptureTick = Find.TickManager.TicksGame
                + MunoStoryUtility.DaysToTicks(MunoStoryConfigDef.Current.recordingIntervalDays.RandomInRange);
        }

        //接收任务成功完成的片段，达到整带要求后在摄影者附近放置录像带。
        public void RecordClip(Pawn wearer)
        {
            MunoStoryConfigDef config = MunoStoryConfigDef.Current;
            clips = System.Math.Min(clips + 1, config.recordingsPerTape);
            if (clips < config.recordingsPerTape) return;
            Thing tape = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Muno_RecordingTape"));
            if (!GenPlace.TryPlaceThing(tape, wearer.Position, wearer.Map, ThingPlaceMode.Near))
            {
                tape.Destroy();
                Log.Error("缪诺摄像机无法在拍摄者附近放置录像带，已保留完整拍摄进度。");
                return;
            }
            tape.SetForbidden(false);
            clips = 0;
            Messages.Message(wearer.LabelShortCap + " 完成了一份生存录像。", tape, MessageTypeDefOf.PositiveEvent, false);
        }

        //为穿戴者提供上传录像带的唯一摄像机操作入口。
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            Pawn wearer = (parent as Apparel)?.Wearer;
            if (wearer == null || !wearer.IsColonistPlayerControlled) yield break;
            yield return new Command_Action
            {
                defaultLabel = "上传录像带",
                defaultDesc = "向节目组上传地图上未禁用的录像带，换取随机物资。",
                icon = parent.def.uiIcon,
                action = () => MunoRecordingExchange.Open(wearer)
            };
        }

        //展示自动拍摄状态、当前片段进度和拍摄间隔。
        public override string CompInspectStringExtra()
        {
            int remaining = System.Math.Max(0, nextCaptureTick - Find.TickManager.TicksGame);
            return "定期自动拍摄\n录像片段：" + clips + "/" + MunoStoryConfigDef.Current.recordingsPerTape
                + (remaining > 0 ? "\n整理录像：" + remaining.ToStringTicksToPeriod() : "\n可以开始下一段拍摄");
        }

        //保存摄像机的拍摄进度和下一次拍摄时刻。
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref clips, "clips");
            Scribe_Values.Look(ref nextCaptureTick, "nextCaptureTick");
        }
    }
}
