using System.Collections.Generic;
using System.Linq;
using MunoRaceLib.MunoDefRef;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //统一提供剧情使用的种族、人员范围、派系及时间换算。
    public static class MunoStoryUtility
    {
        //返回当前存档的缪诺聚落实例。
        public static Faction Faction => Find.FactionManager.FirstFactionOfDef(MunoDefDataRef.MunoColony_Faction);

        //以种族定义判断缪诺，避免用可变化的身份类型判断。
        public static bool IsMuno(Pawn pawn) => pawn.def == MunoDefDataRef.MunoRace_Colonist.race;

        //判断是否属于结局统计的非缪诺正式成员，包含儿童和奴隶。
        public static bool IsEndingMember(Pawn pawn)
        {
            return pawn != null && !pawn.Dead && !pawn.Destroyed && pawn.RaceProps.Humanlike
                && !IsMuno(pawn) && !pawn.IsQuestLodger() && !pawn.IsPrisoner
                && (pawn.Faction == RimWorld.Faction.OfPlayer || pawn.IsSlaveOfColony);
        }

        //收集各地图、远行队、运输容器和重力飞船中的正式成员。
        public static List<Pawn> EndingMembers()
        {
            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive
                .Where(IsEndingMember).Distinct().ToList();
        }

        //判断队伍是否仍有可以选择融入聚落的缪诺正式成员。
        public static bool HasMunoMember()
        {
            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive.Any(p => !p.Dead && !p.Destroyed
                && p.Faction == RimWorld.Faction.OfPlayer && !p.IsPrisoner && !p.IsQuestLodger() && IsMuno(p));
        }

        //将游戏天数转换成游戏刻数。
        public static int DaysToTicks(float days) => (int)(days * GenDate.TicksPerDay);
    }
}
