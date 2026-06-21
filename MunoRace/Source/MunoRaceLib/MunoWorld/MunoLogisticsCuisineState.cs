namespace MunoRaceLib.MunoWorld
{
    //表示后勤管理员分子料理流程当前状态。
    public enum MunoLogisticsCuisineState
    {
        Available,
        AwaitingTaste,
        PendingFeedback,
        Cooldown
    }

    //表示玩家对分子料理试吃的反馈类型。
    public enum MunoLogisticsCuisineFeedback
    {
        Delicious,
        Ordinary,
        Awful
    }
}
