using MunoRaceLib.MunoComp;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class HediffCompProperties_GalactogenDrain : HediffCompProperties
    {
        public float drainPerHour = 2f;
        public HediffCompProperties_GalactogenDrain()
        {
            compClass = typeof(HediffComp_GalactogenDrain);
        }
    }

    public class HediffComp_GalactogenDrain : HediffComp
    {
        private HediffCompProperties_GalactogenDrain Props => (HediffCompProperties_GalactogenDrain)props;

        public override bool CompShouldRemove
        {
            get
            {
                //如果穿戴者身上没有乳源质组件，或乳源质已归零，则移除 Hediff
                ThingComp_Galactogen galComp = parent.pawn.GetComp<ThingComp_Galactogen>();
                if (galComp == null) return true;
                return galComp.CurrentGalactogen <= galComp.MinGalactogen;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            //每小时消耗一次
            if (parent.pawn.IsHashIntervalTick(2500))
            {
                ThingComp_Galactogen galComp = parent.pawn.GetComp<ThingComp_Galactogen>();
                if (galComp != null)
                {
                    galComp.updateGalactogen(-Props.drainPerHour);
                }
            }
        }
    }
}