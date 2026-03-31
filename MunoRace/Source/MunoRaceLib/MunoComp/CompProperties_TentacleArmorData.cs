using Verse;

namespace MunoRaceLib.MunoComp
{
    public class CompProperties_TentacleArmorData : CompProperties
    {
        public CompProperties_TentacleArmorData()
        {
            compClass = typeof(Comp_TentacleArmorData);
        }
    }

    public class Comp_TentacleArmorData : ThingComp
    {
        public int wornTicks;
        public bool dependencyTriggered;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref wornTicks, "wornTicks", 0);
            Scribe_Values.Look(ref dependencyTriggered, "dependencyTriggered", false);
        }
    }
}
