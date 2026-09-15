using RimWorld;

namespace MunoRaceLib.MunoQuests
{
    //配置据点情报揭示的世界据点期限、距离和威胁倍率。
    public class CompProperties_MunoOutpostIntel : CompProperties_UseEffect
    {
        public float siteDays = 10f;
        public float threatFactor = 0.7f;
        public int minSiteDistance = 3;
        public int maxSiteDistance = 15;

        //将情报物品的使用效果绑定到派系据点生成器。
        public CompProperties_MunoOutpostIntel()
        {
            compClass = typeof(CompUseEffect_MunoOutpostIntel);
        }
    }
}
