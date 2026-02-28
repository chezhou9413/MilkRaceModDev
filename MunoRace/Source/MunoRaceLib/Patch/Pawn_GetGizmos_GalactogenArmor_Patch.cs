using HarmonyLib;
using MunoRaceLib.MunoComp;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MunoRaceLib.Patch
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_GalactogenArmor_Patch
    {
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            // 1. 先输出原版及其他 Mod 返回的 Gizmo
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            // 2. 只有人类等带有 apparel 追踪器的 Pawn 才需要检查
            if (__instance.apparel != null)
            {
                // 3. 遍历身上所有穿着的衣物
                foreach (Apparel apparel in __instance.apparel.WornApparel)
                {
                    // 尝试获取我们的专属护甲组件
                    ThingComp_GalactogenArmor comp = apparel.GetComp<ThingComp_GalactogenArmor>();
                    if (comp != null)
                    {
                        // 获取并输出我们在组件中定义的 Gizmo
                        foreach (Gizmo armorGizmo in comp.GetArmorGizmos())
                        {
                            yield return armorGizmo;
                        }
                    }
                }
            }
        }
    }
}
