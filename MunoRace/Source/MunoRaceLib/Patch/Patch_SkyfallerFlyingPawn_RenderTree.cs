using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MunoRaceLib.Patch
{
    /// <summary>
    /// 在飞行中的 Pawn 绘制前补齐渲染树，避免 Skyfaller 绘制未初始化 Pawn 时刷错误日志。
    /// </summary>
    [HarmonyPatch(typeof(Skyfaller_FlyingPawn), "DrawAt")]
    public static class Patch_SkyfallerFlyingPawn_RenderTree
    {
        /// <summary>
        /// 确保飞行 Pawn 的 PawnRenderTree 已解析，无法解析时跳过本帧绘制以避免原版 Draw 报错。
        /// </summary>
        public static bool Prefix(Skyfaller_FlyingPawn __instance, Vector3 drawLoc, bool flip)
        {
            Pawn pawn = __instance?.Pawn;
            PawnRenderer renderer = pawn?.Drawer?.renderer;
            if (renderer == null || renderer.renderTree?.Resolved == true)
            {
                return true;
            }

            renderer.EnsureGraphicsInitialized();
            return renderer.renderTree?.Resolved == true;
        }
    }
}
