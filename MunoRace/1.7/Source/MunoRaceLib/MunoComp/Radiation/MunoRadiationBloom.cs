using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.Profile;

namespace MunoRaceLib.MunoComp;

//仅在地面辐射存在时启用专用辉光层，强度和阈值沿用 RimUnity 设置。
[HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
internal static class MunoRadiationBloom
{
    //25 用于缪诺辐射，避开本地其他内容模组使用的 26～29 层。
    internal const int Layer = 25;
    private static readonly HashSet<Mote> Effects = new();

    //第一束辐射开始时启用本层。
    internal static void Register(Mote effect)
    {
        if (Effects.Add(effect) && Effects.Count == 1)
            RimUnity.RimUnityMod.Bloom.SetLayerEnabled(Layer, true);
    }

    //最后一束辐射结束时归还本层控制权。
    internal static void Unregister(Mote effect)
    {
        if (Effects.Remove(effect) && Effects.Count == 0)
            RimUnity.RimUnityMod.Bloom.ReleaseLayer(Layer);
    }

    //退出或读档会直接清空地图，需同时释放未结束的辐射引用。
    [HarmonyPrefix]
    private static void Clear()
    {
        if (Effects.Count == 0) return;
        Effects.Clear();
        RimUnity.RimUnityMod.Bloom.ReleaseLayer(Layer);
    }
}
