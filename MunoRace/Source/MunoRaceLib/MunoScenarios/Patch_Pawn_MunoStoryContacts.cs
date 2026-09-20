using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //为剧情开局中的殖民者提供聚落联络终端，支持前期上交与结局流程。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_MunoStoryContacts
    {
        //保留原有操作，并为可操作的剧情殖民者追加通讯按钮。
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;
            if (!__instance.Spawned || !__instance.IsColonistPlayerControlled || __instance.Downed
                || Current.Game.GetComponent<MunoStoryComponent>().mode == MunoStartMode.None) yield break;
            yield return new Command_Action
            {
                defaultLabel = "联络缪诺聚落",
                defaultDesc = "使用剧情联络终端办理聚落事务、上交录像或申请融入聚落。",
                icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/MunoLogo"),
                action = () => MunoStoryContacts.Open(__instance)
            };
        }
    }
}
