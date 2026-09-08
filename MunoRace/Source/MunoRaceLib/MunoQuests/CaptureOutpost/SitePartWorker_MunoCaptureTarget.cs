using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace MunoRaceLib.MunoQuests
{
    //在原版强盗营地生成后投放任务指定目标并加入守军防御逻辑。
    public class SitePartWorker_MunoCaptureTarget : SitePartWorker
    {
        //将部件持有的目标转移到营地中央可站立地格，仅投放一次。
        public override void PostMapGenerate(Map map)
        {
            Site site = (Site)map.Parent;
            SitePart part = site.parts.Single(p => p.def == def);
            if (part.things == null || !part.things.Any) return;
            Pawn pawn = (Pawn)part.things[0];
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 30, c => c.Standable(map), out IntVec3 cell))
                throw new InvalidOperationException("哨站中央没有可投放抓捕目标的地格。");
            if (!part.things.TryDrop(pawn, cell, map, ThingPlaceMode.Near, out Thing placed))
                throw new InvalidOperationException("无法将指定抓捕目标从哨站容器放入地图。");
            Lord defender = map.mapPawns.AllPawnsSpawned.Where(p => p != pawn && p.Faction == site.Faction)
                .Select(p => p.GetLord()).FirstOrDefault(lord => lord != null);
            if (defender != null) defender.AddPawn(pawn);
            else LordMaker.MakeNewLord(site.Faction, new LordJob_DefendBase(site.Faction, cell, 30000), map, new[] { pawn });
            Find.LetterStack.ReceiveLetter("指定抓捕目标", "请活捉并带回基地：" + pawn.LabelShortCap
                + "。其他人员不能替代任务目标。", LetterDefOf.NeutralEvent, pawn);
        }

        //在世界哨站说明中标明该部件的活捉目标身份。
        public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
        {
            return "指定抓捕目标" + (sitePart.things != null && sitePart.things.Any ? "：" + sitePart.things[0].LabelShortCap : "");
        }
    }
}
