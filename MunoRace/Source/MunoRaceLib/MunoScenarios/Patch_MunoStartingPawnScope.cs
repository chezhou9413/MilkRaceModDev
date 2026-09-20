using System;
using System.Linq;
using HarmonyLib;
using Verse;

namespace MunoRaceLib.MunoScenarios
{
    //标记剧情人物生成的调用范围，让默认请求保留已配置的种族与性别。
    [HarmonyPatch(typeof(StartingPawnUtility), nameof(StartingPawnUtility.NewGeneratedStartingPawn))]
    public static class Patch_MunoStartingPawnScope
    {
        [ThreadStatic] public static int? PawnIndex;

        //记录当前重随机的人物位置，只作用于本模组的剧情开局。
        [HarmonyPrefix]
        public static void Prefix(int index, out int? __state)
        {
            __state = PawnIndex;
            PawnIndex = index >= 0 && Find.Scenario.AllParts.OfType<ScenPart_MunoStart>().Any() ? (int?)index : null;
        }

        //恢复外层生成范围并保留原始异常，不影响其他人物生成流程。
        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception, int? __state)
        {
            PawnIndex = __state;
            return __exception;
        }
    }
}
