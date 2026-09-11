using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //保存任务据点的守军名册与情报引用，按身份和位置统计可交付俘虏。
    public class MunoCaptureRoster : IExposable
    {
        private List<Pawn> defenders = new List<Pawn>();
        private Thing intelligence;
        private bool registered;
        private bool intelligenceRecovered;

        //返回是否已经生成过据点守军。
        public bool Registered => registered;

        //返回是否在交付前将本任务情报带回过玩家基地。
        public bool IntelligenceRecovered => intelligenceRecovered;

        //登记只生成一次的情报物品，其实体由据点或人物背包持有。
        public void SetIntelligence(Thing item)
        {
            intelligence = item;
        }

        //记录据点实际生成的成年守军，并将预先生成的情报交给随机一名守军携带。
        public void RegisterDefenders(Site site, SitePart part, int requiredCount)
        {
            List<Pawn> generated = site.Map.mapPawns.AllPawnsSpawned.Where(pawn =>
                pawn.Faction == site.Faction && pawn.RaceProps.Humanlike && pawn.DevelopmentalStage.Adult()).ToList();
            if (generated.Count < requiredCount)
                throw new InvalidOperationException("据点成年守军人数不足，无法满足本次抓捕数量。");
            foreach (Pawn pawn in generated)
                if (!defenders.Contains(pawn)) defenders.Add(pawn);
            registered = true;
            if (part.things != null && part.things.Any)
            {
                Pawn carrier = generated.RandomElement();
                if (!carrier.inventory.innerContainer.TryAddOrTransfer(part.things[0]))
                    throw new InvalidOperationException("无法将据点情报放入守军背包。");
            }
        }

        //返回当前基地内来自任务据点、存活且仍为玩家囚犯的所有成年人员。
        public List<Pawn> AvailableAt(Map map)
        {
            return defenders.Where(pawn => pawn != null && !pawn.Dead && !pawn.Destroyed
                && pawn.IsPrisonerOfColony && pawn.MapHeld == map).ToList();
        }

        //判断尚在据点或已被玩家活捉的人员是否仍足以完成任务。
        public bool HasEnoughPossibleTargets(Site site, int requiredCount)
        {
            return defenders.Count(pawn => pawn != null && !pawn.Dead && !pawn.Destroyed
                && (pawn.IsPrisonerOfColony || (site != null && site.HasMap
                    && pawn.MapHeld == site.Map && pawn.Faction == site.Faction))) >= requiredCount;
        }

        //辨认任务守军，供任务保留世界引用及显示说明。
        public bool Contains(Pawn pawn)
        {
            return defenders.Contains(pawn);
        }

        //记录物品实际抵达玩家基地的事件，物品仍保留给玩家继续使用。
        public void CheckIntelligenceReturned()
        {
            if (intelligence != null && !intelligence.Destroyed && MunoQuestUtility.ValidHome(intelligence.MapHeld))
                intelligenceRecovered = true;
        }

        //在使用并消耗情报前记录其所在地，避免同一帧使用物品漏记带回奖励。
        public void NotifyIntelligenceUsed(Thing item, Map usedMap)
        {
            if (item == intelligence && MunoQuestUtility.ValidHome(usedMap)) intelligenceRecovered = true;
        }

        //保存守军及物品引用和附加目标结果，不重复持有地图中的实体。
        public void ExposeData()
        {
            Scribe_Collections.Look(ref defenders, "defenders", LookMode.Reference);
            Scribe_References.Look(ref intelligence, "intelligence");
            Scribe_Values.Look(ref registered, "registered");
            Scribe_Values.Look(ref intelligenceRecovered, "intelligenceRecovered");
        }
    }
}
