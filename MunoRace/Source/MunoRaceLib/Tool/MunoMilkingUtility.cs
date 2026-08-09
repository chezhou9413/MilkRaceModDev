using MunoRaceLib.MunoDefRef;
using Verse;

namespace MunoRaceLib.Tool
{
    //统一为成功产出缪诺乳的小人刷新挤奶心情记忆。
    public static class MunoMilkingUtility
    {
        //为拥有心情需求的产出者刷新一天的挤奶愉悦记忆。
        public static void GiveMilkingMood(Pawn pawn)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || MunoDefDataRef.Muno_MilkedMood == null)
            {
                return;
            }

            pawn.needs.mood.thoughts.memories.TryGainMemoryFast(MunoDefDataRef.Muno_MilkedMood);
        }
    }
}
