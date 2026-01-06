using RuntimeAudioClipLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MunoRaceLib.MunoComp
{
    public class ThingCompProperties_Galactogen: CompProperties
    {
        public float maxGalactogen = 50f;
        public float minGalactogen = 0f;
        public float houseGalactogen = 2f;
        public string GalactogenUIName;
        public string GalactogenUIDes;
        public ThingCompProperties_Galactogen()
        {
            this.compClass = typeof(ThingComp_Galactogen);
        }
    }
    public class ThingComp_Galactogen:ThingComp
    {
        public float MaxGalactogen = 50f;
        public float MinGalactogen = 0f;
        public float HouseGalactogen = 2f;
        public float CurrentGalactogen = 0f;
        public float AutoGather = 0.8f;
        public Pawn SelfPawn => parent as Pawn;
        public ThingCompProperties_Galactogen Props => (ThingCompProperties_Galactogen)this.props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            this.MaxGalactogen = Props.maxGalactogen;
            this.MinGalactogen = Props.minGalactogen;
            this.HouseGalactogen = Props.houseGalactogen;
        }

        public override void CompTick()
        {
            base.CompTick();
            //一小时执行一次
            if (parent.IsHashIntervalTick(2500))
            {
                CheckGalactogen();
            }
        }

        private void CheckGalactogen()
        {
            if (SelfPawn.needs != null && SelfPawn.needs.food != null)
            {
                float curPct = SelfPawn.needs.food.CurLevelPercentage;   // 当前饱食度百分比 (0.0 - 1.0)
                if(curPct > 0.25f)
                {
                    updateGalactogen(HouseGalactogen);
                    return;
                }
                if(curPct <= 0.25f && CurrentGalactogen > 0)
                {
                    updateGalactogen(-10);
                    SelfPawn.needs.food.CurLevelPercentage+=0.01f;
                    return;
                }
            }
        }
        public void updateGalactogen(float value)
        {
            this.CurrentGalactogen += value;
            if (CurrentGalactogen < MinGalactogen)
            {
                this.CurrentGalactogen = MinGalactogen;
            }
            if (CurrentGalactogen > MaxGalactogen*1.2f)
            {
                this.CurrentGalactogen = MaxGalactogen*1.2f;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref MaxGalactogen, "MaxGalactogen",50f);
            Scribe_Values.Look(ref MinGalactogen, "MinGalactogen",0f);
            Scribe_Values.Look(ref HouseGalactogen, "HouseGalactogen",2f);
            Scribe_Values.Look(ref CurrentGalactogen, "CurrentGalactogen", 0f);
            Scribe_Values.Look(ref AutoGather, "AutoGather", 0.8f);
        }
    }
}
