using Verse;

namespace MunoRaceLib.MunoBiotech.Health
{
    //使喷射涂层可以被医疗护理清除，也可以随时间自行消退。
    public class Hediff_MunoCoating : HediffWithComps
    {
        //完成护理后清除表面涂层，已造成的伤口仍需单独治疗。
        public override void Tended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.Tended(quality, maxQuality, batchPosition);
            pawn.health.RemoveHediff(this);
        }
    }
}
