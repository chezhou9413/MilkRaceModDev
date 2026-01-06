using HarmonyLib;
using MunoRaceLib.MunoComp;
using MunoRaceLib.MunoGizmo;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MunoRaceLib.Patch
{
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch("GetGizmos")]
    public static class Patch_GalactogenBar_GetGizmos
    {
        static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance != null && Find.Selector.NumSelected < 2)
            {
                var comp = __instance.TryGetComp<ThingComp_Galactogen>();
                if (comp != null)
                {
                    var list = new List<Gizmo>(__result);
                    if (__instance.IsColonistPlayerControlled)
                    {
                        list.Add(new Gizmo_GalactogenBar(__instance));
                    }
                    if (Prefs.DevMode)
                    {
                        list.Add(CreateDevSetButton(comp, 0f));
                        list.Add(CreateDevSetButton(comp, 0.25f));
                        list.Add(CreateDevSetButton(comp, 0.75f));
                        list.Add(CreateDevSetButton(comp, 1f));
                        list.Add(CreateDevSetButton(comp, 1.2f));
                    }
                    __result = list;
                }
            }
        }

        /// <summary>
        /// 创建开发者调试按钮的快捷方法
        /// </summary>
        private static Command_Action CreateDevSetButton(ThingComp_Galactogen comp, float percent)
        {
            return new Command_Action
            {
                defaultLabel = $"DEV: {percent:P0}", // 显示为 DEV: 0%, DEV: 25% 等
                defaultDesc = $"[调试] 将{comp.Props.GalactogenUIName}设置为最大值的 {percent:P0}",
                action = delegate
                {
                    comp.CurrentGalactogen = comp.MaxGalactogen * percent;
                }
            };
        }
    }
}
