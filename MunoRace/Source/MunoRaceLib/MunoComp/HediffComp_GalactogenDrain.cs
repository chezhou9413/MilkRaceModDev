using MunoRaceLib.MunoComp;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //定义 Hediff 每小时消耗乳源质的数量。
    public class HediffCompProperties_GalactogenDrain : HediffCompProperties
    {
        public float drainPerHour = 2f;

        //绑定乳源质持续消耗组件类型。
        public HediffCompProperties_GalactogenDrain()
        {
            compClass = typeof(HediffComp_GalactogenDrain);
        }
    }

    //按小时扣除乳源质，并向界面提供当前效果的消耗数量。
    public class HediffComp_GalactogenDrain : HediffComp
    {
        //返回当前持续消耗效果的 XML 配置。
        private HediffCompProperties_GalactogenDrain Props => (HediffCompProperties_GalactogenDrain)props;

        //返回当前效果每小时消耗的乳源质数量。
        public float DrainPerHour => Props.drainPerHour;

        //乳源质组件不存在或资源耗尽时结束当前效果。
        public override bool CompShouldRemove
        {
            get
            {
                ThingComp_Galactogen galComp = parent.pawn.GetComp<ThingComp_Galactogen>();
                if (galComp == null)
                {
                    return true;
                }

                return galComp.CurrentGalactogen <= galComp.MinGalactogen;
            }
        }

        //每个游戏小时从穿戴者身上扣除配置的乳源质数量。
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
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
