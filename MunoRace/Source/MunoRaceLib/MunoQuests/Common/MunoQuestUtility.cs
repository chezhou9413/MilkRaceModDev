using System.Linq;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace MunoRaceLib.MunoQuests
{
    //统一检查军事任务所需地图、派系、头衔与同类任务占用情况。
    public static class MunoQuestUtility
    {
        //返回可用的地面玩家基地，优先采用任务指定地图。
        public static Map HomeMap(Slate slate)
        {
            Map map = slate.Get<Map>("map");
            return ValidHome(map) ? map : Find.Maps.FirstOrDefault(ValidHome);
        }

        //判定地图是否仍属于玩家并位于地表。
        public static bool ValidHome(Map map)
        {
            return map != null && Find.Maps.Contains(map) && map.IsPlayerHome
                && map.Tile.LayerDef == PlanetLayerDefOf.Surface;
        }

        //查找缪诺委托派系。
        public static Faction Giver()
        {
            return Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);
        }

        //筛选能生成成年战斗人员且同时敌对玩家和缪诺的常驻人类派系。
        public static bool EligibleEnemy(Faction faction)
        {
            return !faction.IsPlayer && !faction.defeated && !faction.temporary && !faction.Hidden
                && faction.def.humanlikeFaction && faction.HostileTo(Faction.OfPlayer)
                && Giver() != null && faction.HostileTo(Giver())
                && faction.def.pawnGroupMakers != null
                && faction.def.pawnGroupMakers.Any(group => group.kindDef == PawnGroupKindDefOf.Combat
                    && group.options.Any(option => option.kind.RaceProps.Humanlike))
                && faction.def.pawnGroupMakers.Any(group => group.kindDef == PawnGroupKindDefOf.Settlement);
        }

        //检查生成邀请所需前提，调试只跳过头衔与自然难度限制。
        public static bool CanOffer(string defName, Slate slate, out string reason)
        {
            reason = null;
            QuestScriptDef def = DefDatabase<QuestScriptDef>.GetNamed(defName);
            if (HomeMap(slate) == null) reason = "没有有效的地面玩家基地。";
            else if (!ModsConfig.RoyaltyActive) reason = "缪诺军事支线需要皇权头衔和穿梭机。";
            else if (Giver() == null || Giver().defeated || Giver().HostileTo(Faction.OfPlayer)) reason = "缪诺派系不存在、已覆灭或与玩家敌对。";
            else if (!Find.FactionManager.AllFactions.Any(EligibleEnemy)) reason = "没有同时敌对玩家和缪诺的可用人类战斗派系。";
            else if (Find.QuestManager.QuestsListForReading.Any(q => q.root == def && !q.Historical)) reason = "已有同类邀请或进行中的任务。";
            else if (!slate.Get<bool>("munoDebug"))
            {
                RoyalTitleDef title = DefDatabase<RoyalTitleDef>.GetNamed(def.GetModExtension<MunoQuestConfig>().requiredTitle);
                if (!Find.Storyteller.difficulty.allowViolentQuests) reason = "当前难度不允许战斗任务。";
                else if (!PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists.Any(p =>
                    p.royalty?.GetCurrentTitle(Giver()) is RoyalTitleDef current && current.seniority >= title.seniority))
                    reason = "没有自由殖民者持有所需的缪诺头衔。";
            }
            return reason == null;
        }

        //查找适合创建抓捕哨站的邻近可达地块。
        public static bool FindSiteTile(Map map, out PlanetTile tile)
        {
            return TileFinder.TryFindNewSiteTile(out tile, map.Tile, minDist: 3, maxDist: 15,
                canBeSpace: false, layer: map.Tile.Layer);
        }

        //向玩家显示无法执行调试动作的具体原因。
        public static void Reject(string reason)
        {
            Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
        }
    }
}
