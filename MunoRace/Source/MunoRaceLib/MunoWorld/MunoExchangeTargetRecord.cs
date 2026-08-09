using Verse;

namespace MunoRaceLib.MunoWorld
{
    //保存一名交换目标及其对应的奖励选择，并支持交换会话存档。
    public class MunoExchangeTargetRecord : IExposable
    {
        public Pawn pawn;
        public MunoExchangeRewardType rewardType = MunoExchangeRewardType.MunoPawn;

        //创建空的交换目标记录，供存档系统恢复引用。
        public MunoExchangeTargetRecord()
        {
        }

        //创建绑定目标 Pawn 和奖励类型的交换目标记录。
        public MunoExchangeTargetRecord(Pawn pawn, MunoExchangeRewardType rewardType)
        {
            this.pawn = pawn;
            this.rewardType = rewardType;
        }

        //持久化交换目标引用和奖励类型。
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "munoExchangePawn");
            Scribe_Values.Look(ref rewardType, "munoExchangeRewardType", MunoExchangeRewardType.MunoPawn);
        }
    }
}
