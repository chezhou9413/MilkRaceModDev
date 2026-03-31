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
    public static class MunoDefDataRef
    {
        public static ThingDef MunoRace_MunoMilk;
        public static ThingDef MunoRace_ConcentratedMulacte;

        public static JobDef JobDriver_SpawnMunoMilk;
        public static JobDef JobDriver_SpawnConcentratedMulacte;

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

        public static ThoughtDef Muno_TentacleBioLiningPleasure;
        public static ThoughtDef Muno_TentacleBioLiningPleasureStrong;
        public static ThoughtDef Muno_TentacleWithdrawalThought;

        public static ThingDef MunoRace_Apparel_Tentaclearmor;
        public static ThingDef MunoRace_Apparel_Tentaclefrenzyarmor;

        public static PawnKindDef Muno_Mech_Tentacles;

        public static JobDef JobDef_RefuelGalactogenArmor;
    }
}
