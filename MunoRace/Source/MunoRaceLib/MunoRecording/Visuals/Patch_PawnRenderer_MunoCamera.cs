using HarmonyLib;
using Verse;

namespace MunoRaceLib.MunoRecording
{
    //在动态拍摄阶段停用人物远距贴图缓存，让举机动画在不同缩放下持续更新。
    [HarmonyPatch(typeof(PawnRenderer), "ParallelGetPreRenderResults")]
    public static class Patch_PawnRenderer_MunoCamera
    {
        //仅对正在取景的人物使用实时渲染，不替换人物原有动画。
        [HarmonyPrefix]
        public static void Prefix(Pawn ___pawn, ref bool disableCache)
        {
            if (___pawn.jobs?.curDriver is JobDriver_MunoRecordColonist driver && driver.Filming) disableCache = true;
        }
    }
}
