using RimWorld;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoRecording
{
    //在人物身体节点下按朝向绘制摄像机，并插值表现举机、取景微动和收机。
    public class PawnRenderNodeWorker_MunoCamera : PawnRenderNodeWorker
    {
        //只在显示服装且人物站立时绘制摄像机，保留肖像中的佩戴状态。
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.CanDrawNow(node, parms) && parms.flags.FlagSet(PawnRenderFlags.Clothes)
                && (parms.Portrait || parms.posture == PawnPosture.Standing);
        }

        //依据拍摄进度生成从腰侧到胸前的连续举机位置。
        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 offset = base.OffsetFor(node, parms, out pivot);
            float raised = RaisedAmount(parms);
            float side = parms.facing == Rot4.West ? -1f : 1f;
            Vector3 resting = new Vector3(0.24f * side, 0f, -0.16f);
            Vector3 aiming = parms.facing.IsHorizontal
                ? new Vector3(0.36f * side, 0f, 0.12f)
                : new Vector3(0.12f * side, 0f, parms.facing == Rot4.North ? 0.26f : 0.06f);
            offset += Vector3.Lerp(resting, aiming, raised);
            if (raised > 0.9f && !parms.Portrait)
                offset.z += Mathf.Sin(Find.TickManager.TicksGame * 0.09f) * 0.012f;
            return offset;
        }

        //让北向摄像机位于头部后侧，其余朝向位于衣服前侧。
        public override float LayerFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return parms.facing == Rot4.North ? 45f : 95f;
        }

        //将贴图顶部的镜头转向人物正前方，佩戴和取景阶段保持相同朝向。
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.RotationFor(node, parms) * parms.facing.AsQuat;
        }

        //从当前拍摄任务读取举机程度，肖像和非拍摄状态使用静态佩戴姿态。
        private static float RaisedAmount(PawnDrawParms parms)
        {
            if (parms.Portrait || !(parms.pawn.jobs?.curDriver is JobDriver_MunoRecordColonist driver) || !driver.Filming) return 0f;
            float progress = driver.Progress;
            float blend = progress < 0.2f ? progress / 0.2f : progress > 0.8f ? (1f - progress) / 0.2f : 1f;
            return Mathf.SmoothStep(0f, 1f, blend);
        }
    }
}
