using HarmonyLib;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //在种族框架重新索取默认人物时返回当前剧情位置的固定请求。
    [HarmonyPatch(typeof(StartingPawnUtility), "DefaultStartingPawnRequest", MethodType.Getter)]
    public static class Patch_MunoStartingPawnRequest
    {
        //保留人物准备页面的已有请求，防止重随机改变剧本人员构成。
        [HarmonyPostfix]
        public static void Postfix(ref PawnGenerationRequest __result)
        {
            if (Patch_MunoStartingPawnScope.PawnIndex.HasValue)
                __result = StartingPawnUtility.GetGenerationRequest(Patch_MunoStartingPawnScope.PawnIndex.Value);
        }
    }
}
