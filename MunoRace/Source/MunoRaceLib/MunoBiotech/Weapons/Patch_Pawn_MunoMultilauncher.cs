using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace MunoRaceLib.MunoBiotech.Weapons
{
    //为装备多功能发射器的小人追加模式切换按钮。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_MunoMultilauncher
    {
        //保留已有指令，武器浓浆槽仍由现有装备界面显示。
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result)
                yield return gizmo;
            Comp_MunoMultilauncher launcher = __instance.equipment?.Primary?.GetComp<Comp_MunoMultilauncher>();
            if (launcher != null && __instance.IsColonistPlayerControlled)
                yield return launcher.ModeGizmo();
        }
    }
}
