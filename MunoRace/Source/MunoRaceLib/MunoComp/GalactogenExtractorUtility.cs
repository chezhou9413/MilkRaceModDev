using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //集中判断小人是否正在穿戴可用的乳源质自动汲取装备。
    public static class GalactogenExtractorUtility
    {
        //返回指定小人当前是否穿戴了汲取内衬或挤奶器。
        public static bool HasActiveExtractor(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return false;
            }

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel.GetComp<Comp_GalactogenExtractor>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
