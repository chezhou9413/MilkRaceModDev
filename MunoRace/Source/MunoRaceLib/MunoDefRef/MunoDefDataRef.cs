using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MunoRaceLib.MunoDefRef
{
    [DefOf]
    //集中缓存本模组常用 Def，供 C# 逻辑安全引用 XML 定义。
    public static class MunoDefDataRef
    {
        public static ThingDef MunoRace_MunoMilk;
        public static ThingDef MunoRace_ConcentratedMulacte;
        public static ThingDef MunoRace_MolecularCuisine;
        public static ThingDef Bullet_MunoAC_Gel;
        public static ThingDef Bullet_MunoSR_Grenade;

        public static AbilityDef Muno_GalactogenGelShot;
        public static AbilityDef Muno_StrategicGrenadeShot;
        public static AbilityDef Muno_LaserSweep_LR;
        public static AbilityDef Muno_LaserSweep_LSR;

        public static JobDef JobDriver_SpawnMunoMilk;
        public static JobDef JobDriver_SpawnConcentratedMulacte;
        public static JobDef JobDef_GalactogenGelShot;
        public static JobDef JobDef_MunoStrategicGrenadeShot;
        public static JobDef JobDef_MunoLaserSweep;

        public static ThingDef Muno_Filth_Galactogen;

        public static FleckDef Muno_Fleck_MilkSplatter;

        public static StatDef Muno_MaxGalactogen;
        public static StatDef Muno_GalactogenRecovery;

        public static HediffDef Muno_GalactogenEnhancement;
        public static HediffDef Muno_TentacleNervePassive;
        public static HediffDef Muno_TentacleBioLiningActive;
        public static HediffDef Muno_TentacleNerveActive;
        public static HediffDef Muno_TentacleBioLiningPassive;
        public static HediffDef Muno_TentacleWithdrawal;
        public static HediffDef Muno_TentacleMinionLifetime;
        public static HediffDef Muno_GelSlowdown;
        public static HediffDef Muno_MolecularCuisineConsciousnessUp;
        public static HediffDef Muno_MolecularCuisineConsciousnessDown;
        public static HediffDef Muno_MolecularCuisineConsciousnessPermanent;

        public static ThoughtDef Muno_TentacleBioLiningPleasure;
        public static ThoughtDef Muno_TentacleBioLiningPleasureStrong;
        public static ThoughtDef Muno_TentacleWithdrawalThought;
        public static ThoughtDef Muno_MolecularCuisineDelicious;
        public static ThoughtDef Muno_MolecularCuisineAwful;

        public static ThingDef MunoRace_Disabler;
        public static ThingDef MunoRace_Apparel_Tentaclearmor;
        public static ThingDef MunoRace_Apparel_Tentaclefrenzyarmor;
        public static ThingDef MunoRace_Apparel_TentaclefrenzyarmorHead;

        public static PawnKindDef Muno_Mech_Tentacles;
        public static PawnKindDef MunoRace_Colonist;
        public static PawnKindDef MunoRace_MarriagePrincess;

        public static JobDef JobDef_RefuelGalactogenArmor;
        public static JobDef JobDef_RefuelGalactogenWeapon;
        public static JobDef JobDef_ReloadStrategicGrenade;

        public static FactionDef MunoColony_Faction;
    }
}
