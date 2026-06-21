using MunoRaceLib.MunoWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //负责监听分子料理被吃掉或非正常销毁后的后勤通讯状态变化。
    public class Comp_MolecularCuisine : ThingComp
    {
        private bool ingesting;
        private bool tasted;

        //保存或读取料理组件的临时追踪状态。
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ingesting, "ingesting", defaultValue: false);
            Scribe_Values.Look(ref tasted, "tasted", defaultValue: false);
        }

        //在食用结算前标记本次销毁来自食用流程。
        public override void PrePostIngested(Pawn ingester)
        {
            base.PrePostIngested(ingester);
            ingesting = true;
        }

        //在食用完成后结算随机效果并解锁反馈。
        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);
            tasted = true;
            MunoLogisticsCuisineComponent.Current()?.NotifyCuisineTasted(parent, ingester);
        }

        //在非食用销毁时通知后勤组件处理补发或冷却。
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (ingesting || tasted)
            {
                ingesting = false;
                return;
            }

            MunoLogisticsCuisineComponent.Current()?.NotifyCuisineLost(parent, previousMap);
        }
    }

    //负责把分子料理组件挂到 ThingDef 上。
    public class CompProperties_MolecularCuisine : CompProperties
    {
        //初始化分子料理组件配置。
        public CompProperties_MolecularCuisine()
        {
            compClass = typeof(Comp_MolecularCuisine);
        }
    }
}
