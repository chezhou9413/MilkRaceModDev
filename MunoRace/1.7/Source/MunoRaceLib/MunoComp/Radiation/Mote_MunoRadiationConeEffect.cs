using System.Collections.Generic;
using MunoRaceLib.MunoDefRef;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp;

//管理一次辐射爆发的独立网格、播放时钟和辉光层生命周期。
[StaticConstructorOnStartup]
public class Mote_MunoRadiationConeEffect : Mote
{
    private static readonly int EffectAgeId = Shader.PropertyToID("_EffectAge");
    private static readonly MaterialPropertyBlock PropertyBlock = new();
    private Mesh burstMesh;
    private Matrix4x4 drawMatrix;
    private Vector2 drawSize;

    //让原版绘制裁剪覆盖完整束带和命中点。
    public override Vector2 DrawSize => drawSize;

    //在伤害结算前记录命中位置，死亡或建筑破坏不改变已发出的爆发。
    public static void Spawn(Map map, MunoRadiationConeGeometry geometry, IEnumerable<Thing> targets)
    {
        Mote_MunoRadiationConeEffect effect =
            (Mote_MunoRadiationConeEffect)ThingMaker.MakeThing(MunoDefDataRef.Mote_MunoRadiationConeEffect);
        effect.burstMesh = MunoRadiationBurstGeometry.Build(map, geometry, targets, effect.thingIDNumber);
        effect.drawMatrix = Matrix4x4.TRS(geometry.Origin, Quaternion.identity, Vector3.one);
        Bounds bounds = effect.burstMesh.bounds;
        effect.exactPosition = geometry.Origin + bounds.center;
        effect.drawSize = new Vector2(bounds.size.x, bounds.size.z);
        GenSpawn.Spawn(effect, geometry.Origin.ToIntVec3(), map);
    }

    //登记这一束的辉光，不修改玩家的全局后处理设置。
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        MunoRadiationBloom.Register(this);
    }

    //所有束带、火花和冲击波只需一次绘制与一次时钟更新。
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (Find.UIRoot.HideMotes) return;
        PropertyBlock.Clear();
        PropertyBlock.SetFloat(EffectAgeId, AgeSecs);
        Graphics.DrawMesh(burstMesh, drawMatrix, Graphic.MatSingle, MunoRadiationBloom.Layer, null, 0, PropertyBlock);
    }

    //正常播放结束时释放独立网格。
    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        Release();
    }

    //卸载地图也会结束仍在播放的爆发。
    public override void Notify_MyMapRemoved()
    {
        base.Notify_MyMapRemoved();
        Release();
    }

    //地图移除与销毁可能先后发生，网格只释放一次。
    private void Release()
    {
        MunoRadiationBloom.Unregister(this);
        burstMesh?.Dispose();
        burstMesh = null;
    }
}
