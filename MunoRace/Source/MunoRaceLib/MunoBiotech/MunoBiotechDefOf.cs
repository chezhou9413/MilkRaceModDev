using RimWorld;
using Verse;

namespace MunoRaceLib.MunoBiotech
{
    //缓存乳制品加工、药液与三种喷射涂层的定义。
    [DefOf]
    public static class MunoBiotechDefOf
    {
        public static ThingDef MunoRace_MunoWhey;
        public static ThingDef Bullet_MunoStrongGel;
        public static ThingDef Bullet_MunoAcidGel;
        public static ThingDef Bullet_MunoMedicalFoam;
        public static HediffDef Muno_MilkMedicine;
        public static HediffDef Muno_StrongGelCoating;
        public static HediffDef Muno_AcidGelCoating;
        public static HediffDef Muno_MedicalFoamCoating;
        public static HediffDef Muno_AcidBurn;

        //确保在生产或战斗逻辑首次使用前完成定义绑定。
        static MunoBiotechDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MunoBiotechDefOf));
        }
    }
}
