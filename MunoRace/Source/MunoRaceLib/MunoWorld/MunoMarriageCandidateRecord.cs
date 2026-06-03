using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责保存单名和亲公主的存档追踪状态。
    /// </summary>
    public class MunoMarriageCandidateRecord : IExposable
    {
        public Pawn pawn;
        public bool accepted;
        public bool delivered;
        public bool marriageRewardGiven;
        public bool abusePenaltyGiven;

        /// <summary>
        /// 保存或读取和亲公主记录中的 Pawn 引用与后续事件状态。
        /// </summary>
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref accepted, "accepted", defaultValue: false);
            Scribe_Values.Look(ref delivered, "delivered", defaultValue: false);
            Scribe_Values.Look(ref marriageRewardGiven, "marriageRewardGiven", defaultValue: false);
            Scribe_Values.Look(ref abusePenaltyGiven, "abusePenaltyGiven", defaultValue: false);
        }
    }
}
