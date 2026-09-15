using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //在原版营地完成生成后登记任务守军，并投放本次预先抽取的情报。
    public class SitePartWorker_MunoCaptureOutpost : SitePartWorker
    {
        //把当前据点交回对应任务，由任务记录可抓捕人员并处理生成异常。
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            Site site = (Site)map.Parent;
            QuestPart_MunoCapture mission = Find.QuestManager.QuestsListForReading
                .Where(quest => quest.State == QuestState.Ongoing)
                .SelectMany(quest => quest.PartsListForReading).OfType<QuestPart_MunoCapture>()
                .SingleOrDefault(part => part.Site == site);
            //任务结束后仍可能保留玩家正在使用的据点地图，此时不再登记任务人员。
            if (mission != null) mission.RegisterOutpost(site.parts.Single(part => part.def == def));
        }

        //在世界据点面板说明这里允许按人数抓捕守军。
        public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
        {
            return "活捉任意成年守军";
        }
    }
}
