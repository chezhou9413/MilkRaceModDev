using Verse;

namespace MunoRaceLib.MunoQuests
{
    //保存军事支线的头衔门槛、阶段时限、战斗强度和奖励预算。
    public class MunoQuestConfig : DefModExtension
    {
        public string requiredTitle;
        public float captureDays = 15f;
        public FloatRange arrivalDays = new FloatRange(1f, 2f);
        public float shuttleDays = 3f;
        public float rewardValue = 2000f;
        public float threatFactor = 1f;
        public IntRange combatSkill = new IntRange(14, 18);
        public IntRange injuryCount = new IntRange(3, 6);
        public FloatRange injuryDamage = new FloatRange(3f, 6f);
    }
}
