using MunoRaceLib.MunoComp;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoThoughtWorker
{
    public class ThoughtWorker_GalactogenOverflow : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var comp = p.TryGetComp<ThingComp_Galactogen>();
            if (comp == null || comp.MaxGalactogen <= 0)
            {
                return ThoughtState.Inactive;
            }
            float percentage = comp.CurrentGalactogen / comp.MaxGalactogen;
            if (percentage >= 1.2f)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            if (percentage > 1.0f)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }
}
