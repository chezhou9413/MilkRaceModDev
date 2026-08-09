using Verse;

namespace MunoRaceLib.MunoComp
{
    //提供可由 AbilityDef XML 调整的激光炮锥角、伤害、穿透、浓浆消耗与视觉特效参数。
    public class VerbProperties_MunoRadiationCone : VerbProperties
    {
        public float coneAngleDegrees = 22f;
        public int damageAmount = 25;
        public float armorPenetration = 0.8f;
        public int storageCost = 1;
        public DamageDef damageDef;
        public FleckDef coneFleckDef;
        public float coneFleckChancePerCell = 0.28f;
        public FloatRange coneFleckScale = new FloatRange(4f, 7f);
        public FleckDef targetFleckDef;
        public float targetFleckScale = 0.45f;
    }
}
