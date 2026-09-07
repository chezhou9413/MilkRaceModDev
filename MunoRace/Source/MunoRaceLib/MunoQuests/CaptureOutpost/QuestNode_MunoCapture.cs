using RimWorld.QuestGen;

namespace MunoRaceLib.MunoQuests
{
    //将具备可达哨站地块的抓捕委托加入原版任务生成流程。
    public class QuestNode_MunoCapture : QuestNode_MunoMilitary
    {
        //标识抓捕任务定义。
        protected override string ScriptName => "Muno_CaptureOutpost";

        //检查基地附近是否仍有可达且未被占用的任务地块。
        protected override bool TestRunInt(Slate slate)
        {
            return base.TestRunInt(slate) && MunoQuestUtility.FindSiteTile(MunoQuestUtility.HomeMap(slate), out _);
        }

        //建立尚未启动的抓捕进度。
        protected override QuestPart_MunoMilitary CreateMission()
        {
            return new QuestPart_MunoCapture();
        }
    }
}
