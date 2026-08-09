using RimWorld;

namespace MunoRaceLib.MunoComp
{
    //绑定激光炮锥形辐射的原版能力效果组件。
    public class CompProperties_AbilityMunoRadiationCone : CompProperties_AbilityEffect
    {
        //指定辐射放射能力实际执行的效果组件类型。
        public CompProperties_AbilityMunoRadiationCone()
        {
            compClass = typeof(CompAbilityEffect_MunoRadiationCone);
        }
    }
}
