using System.Collections.Generic;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //定义一份录像带可抽取的一种物资及其数量范围和权重。
    public class MunoRecordingRewardDef : Def
    {
        public ThingDef thingDef;
        public IntRange count = new IntRange(1, 1);
        public float weight = 1f;

        //检查奖励是否能够按普通物资直接生成。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors()) yield return error;
            if (thingDef == null || thingDef.MadeFromStuff || thingDef.category != ThingCategory.Item)
                yield return "录像奖励必须是无需指定材质的物品。";
            if (count.min <= 0 || count.max < count.min || weight <= 0f)
                yield return "录像奖励数量和权重必须为正数，区间必须有序。";
        }
    }
}
