using Verse;

namespace MunoRaceLib.MunoComp
{
    public class HediffCompProperties_TentacleMinionLifetime : HediffCompProperties
    {
        public HediffCompProperties_TentacleMinionLifetime()
        {
            compClass = typeof(HediffComp_TentacleMinionLifetime);
        }
    }

    public class HediffComp_TentacleMinionLifetime : HediffComp
    {
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (parent?.pawn != null && !parent.pawn.Destroyed)
            {
                parent.pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
