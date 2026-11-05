using Verse;

namespace MunoRaceLib.MunoBiotech.Health
{
    //让药液作用期间的机械感染以双倍速度走完自然消退时间。
    public class Hediff_MunoMilkMedicine : HediffWithComps
    {
        private int recoveryTicks;

        //仅处理两种原版机械感染，不改变其他限时健康状态。
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.Dead)
                return;
            recoveryTicks += delta;
            if (recoveryTicks < 250)
                return;

            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def.defName != "FibrousMechanites" && hediff.def.defName != "SensoryMechanites")
                    continue;
                HediffComp_Disappears duration = hediff.TryGetComp<HediffComp_Disappears>();
                if (duration != null)
                    duration.ticksToDisappear -= recoveryTicks;
            }
            recoveryTicks = 0;
        }

        //保存尚未结算的恢复时间。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref recoveryTicks, "recoveryTicks");
        }
    }
}
