using Verse;

namespace MunoRaceLib.MunoComp
{
    //标记能够启用双模式自动汲取功能的服装配置。
    public class CompProperties_GalactogenExtractor : CompProperties
    {
        //绑定自动汲取标记组件的运行类型。
        public CompProperties_GalactogenExtractor()
        {
            compClass = typeof(Comp_GalactogenExtractor);
        }
    }
}
