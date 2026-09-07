namespace MunoRaceLib.MunoQuests
{
    //将缪诺受伤突袭队员的庇护邀请接入原版任务生成流程。
    public class QuestNode_MunoProtect : QuestNode_MunoMilitary
    {
        //标识保护任务定义。
        protected override string ScriptName => "Muno_ProtectRaider";

        //建立尚未启动的保护进度。
        protected override QuestPart_MunoMilitary CreateMission()
        {
            return new QuestPart_MunoProtect();
        }
    }
}
