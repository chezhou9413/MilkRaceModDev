using RimWorld;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //负责在缪诺乳被饮用后解除饮用者的原版食物中毒。
    public class Comp_MunoMilkFoodPoisoningCure : ThingComp
    {
        //食用结算完成后移除食物中毒，并在成功解除时通知玩家。
        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);
            Hediff foodPoisoning = ingester.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning);
            if (foodPoisoning == null)
            {
                return;
            }

            ingester.health.RemoveHediff(foodPoisoning);
            Messages.Message(ingester.LabelShort + "的食物中毒已被缪诺乳解除。", ingester, MessageTypeDefOf.PositiveEvent, false);
        }
    }

    //负责把食物中毒解除组件挂载到缪诺乳定义。
    public class CompProperties_MunoMilkFoodPoisoningCure : CompProperties
    {
        //绑定缪诺乳的食物中毒解除组件类型。
        public CompProperties_MunoMilkFoodPoisoningCure()
        {
            compClass = typeof(Comp_MunoMilkFoodPoisoningCure);
        }
    }
}
