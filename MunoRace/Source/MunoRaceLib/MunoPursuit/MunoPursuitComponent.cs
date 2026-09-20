using System.Collections.Generic;
using System.Linq;
using MunoRaceLib.MunoScenarios;
using RimWorld;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoPursuit
{
    //保存原始逃亡者和追击计时，根据队伍位置与外交关系调度缪诺袭击。
    public class MunoPursuitComponent : GameComponent
    {
        private List<Pawn> fugitives = new List<Pawn>();
        private bool active;
        private int nextRaidTick;
        private int waves;

        //创建当前存档的追击状态。
        public MunoPursuitComponent(Game game) { }

        //返回追击是否仍在进行。
        public bool Active => active;

        //绑定初始两名逃亡者并设置首次追击宽限期。
        public void Begin(List<Pawn> pawns)
        {
            fugitives = new List<Pawn>(pawns);
            active = true;
            waves = 0;
            nextRaidTick = Find.TickManager.TicksGame + MunoStoryUtility.DaysToTicks(MunoStoryConfigDef.Current.pursuitGraceDays);
        }

        //处理追击终止条件，并在到期时向逃亡者所在地图派遣袭击。
        public override void GameComponentTick()
        {
            if (!active || Find.TickManager.TicksGame % 250 != 0) return;
            Faction faction = MunoStoryUtility.Faction;
            if (faction == null || faction.defeated || faction.deactivated)
            {
                Stop("缪诺聚落已失去追击能力。");
                return;
            }
            if (!faction.HostileTo(Faction.OfPlayer))
            {
                Stop("你们与缪诺聚落达成和平，追击令已被撤销。");
                return;
            }
            var remaining = fugitives.Where(p => p != null && !p.Dead && !p.Destroyed
                && p.Faction == Faction.OfPlayer).ToList();
            if (remaining.Count == 0)
            {
                Stop("原始逃亡者已全部离开玩家队伍，追击令不再针对这处殖民地。");
                return;
            }
            if (Find.TickManager.TicksGame < nextRaidTick) return;
            Map map = remaining.Select(p => p.MapHeld).FirstOrDefault(m => m != null);
            //逃亡者在远行队中时保留追击到期状态，回到地图后继续追踪。
            if (map == null) return;
            MunoStoryConfigDef config = MunoStoryConfigDef.Current;
            nextRaidTick = Find.TickManager.TicksGame + MunoStoryUtility.DaysToTicks(config.pursuitIntervalDays.RandomInRange);
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.faction = faction;
            parms.forced = true;
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            parms.points = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(map)
                * (1f + waves * config.pursuitGrowthPerWave), config.pursuitPoints.min, config.pursuitPoints.max);
            if (IncidentDefOf.RaidEnemy.Worker.TryExecute(parms)) waves++;
            else Log.Warning("缪诺追击未能在目标地图生成，本轮已结束，将在下个追击周期重新派遣。");
        }

        //撤销追击，并向玩家说明原因。
        public void Stop(string reason)
        {
            if (!active) return;
            active = false;
            Find.LetterStack.ReceiveLetter("缪诺追击结束", reason, LetterDefOf.PositiveEvent);
        }

        //保存逃亡者引用、已完成波数和下一次追击时刻。
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref fugitives, "fugitives", LookMode.Reference);
            Scribe_Values.Look(ref active, "active");
            Scribe_Values.Look(ref nextRaidTick, "nextRaidTick");
            Scribe_Values.Look(ref waves, "waves");
        }
    }
}
