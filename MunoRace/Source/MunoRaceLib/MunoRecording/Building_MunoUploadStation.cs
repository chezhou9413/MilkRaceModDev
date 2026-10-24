using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MunoRaceLib.MunoRecording
{
    //让殖民者选择上传数量，并到上传站完成录像兑换。
    public class Building_MunoUploadStation : Building
    {
        //右键列出当前地图的可上传数量以及不可使用的原因。
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(pawn)) yield return option;
            if (Faction != Faction.OfPlayer || !pawn.IsColonistPlayerControlled) yield break;
            string reason = MunoRecordingExchange.UnavailableReason(pawn);
            if (reason == null && this.IsForbidden(pawn)) reason = "上传站已被禁用";
            if (reason == null && !pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some)) reason = "无法到达上传站";
            if (reason == null && !pawn.CanReserve(this)) reason = "上传站正在使用中";
            int total = MunoRecordingExchange.AvailableTapeCount(Map);
            if (reason == null && total == 0) reason = "地图上没有未禁用的录像带";
            if (reason != null)
            {
                yield return new FloatMenuOption("无法上传录像带：" + reason, null);
                yield break;
            }
            foreach (int amount in new[] { 1, 5, 20, System.Math.Min(total, 50) }.Where(n => n <= total).Distinct().OrderBy(n => n))
            {
                int count = amount;
                yield return new FloatMenuOption("上传 " + count + " 份录像带，领取 " + count + " 份随机奖励", () =>
                {
                    Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Muno_UploadRecording"), this);
                    job.count = count;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        //选中上传站时展示库存和操作方式。
        public override string GetInspectString()
        {
            string basic = base.GetInspectString();
            if (!Spawned) return basic;
            string detail = "可上传录像带：" + MunoRecordingExchange.AvailableTapeCount(Map)
                + "\n选中殖民者后右键此处，选择上传数量。";
            return basic.NullOrEmpty() ? detail : basic + "\n" + detail;
        }
    }
}
