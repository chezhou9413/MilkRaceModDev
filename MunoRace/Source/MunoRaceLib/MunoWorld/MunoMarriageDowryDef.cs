using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoWorld
{
    /// <summary>
    /// 负责保存缪诺和亲嫁妆随机池与数量品质范围配置。
    /// </summary>
    public class MunoMarriageDowryDef : Def
    {
        public List<ThingDef> weaponPool;
        public List<ThingDef> armorPool;
        public List<ThingDef> archotechBodyPartPool;
        public List<ThingDef> bionicFallbackPool;
        public IntRange weaponCount = new IntRange(1, 2);
        public IntRange armorCount = new IntRange(1, 1);
        public IntRange bodyPartCount = new IntRange(1, 3);
        public IntRange techprintCount = new IntRange(1, 1);
        public QualityCategory minQuality = QualityCategory.Excellent;
        public QualityCategory maxQuality = QualityCategory.Masterwork;
    }
}
