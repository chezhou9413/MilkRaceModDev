#if !RW_1_7
using MunoRaceLib.MunoDefRef;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //绘制贴地辐射平面，并把三个锥形顶点与播放进度逐帧传给 CL Shader。
    [StaticConstructorOnStartup]
    public class Mote_MunoRadiationConeEffect : Mote
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int PointAId = Shader.PropertyToID("_PointA");
        private static readonly int PointBId = Shader.PropertyToID("_PointB");
        private static readonly int PointCId = Shader.PropertyToID("_PointC");
#if RW_1_7
        private static readonly int EffectAgeId = Shader.PropertyToID("_EffectAge");
#else
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int CoreBlendId = Shader.PropertyToID("_CoreBlend");
#endif
        private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

        private Vector4 pointA;
        private Vector4 pointB;
        private Vector4 pointC;
        private Vector3 effectCenter;
        private Vector2 effectSize;
#if !RW_1_7
        private float baseEmission;
        private float baseCoreBlend;
#endif

        //返回辐射锥包围面片的实际尺寸，供原版动态绘制系统计算可见区域。
        public override Vector2 DrawSize
        {
            get { return effectSize; }
        }

        //使用辐射锥世界顶点建立包围面片，并把三个顶点换算到面片 UV 空间。
        public void Initialize(MunoRadiationConeGeometry geometry)
        {
            float minX = Mathf.Min(geometry.Origin.x, Mathf.Min(geometry.LeftEnd.x, geometry.RightEnd.x));
            float maxX = Mathf.Max(geometry.Origin.x, Mathf.Max(geometry.LeftEnd.x, geometry.RightEnd.x));
            float minZ = Mathf.Min(geometry.Origin.z, Mathf.Min(geometry.LeftEnd.z, geometry.RightEnd.z));
            float maxZ = Mathf.Max(geometry.Origin.z, Mathf.Max(geometry.LeftEnd.z, geometry.RightEnd.z));
            float width = Mathf.Max(maxX - minX, 0.01f);
            float height = Mathf.Max(maxZ - minZ, 0.01f);

            effectSize = new Vector2(width, height);
            effectCenter = new Vector3(
                (minX + maxX) * 0.5f,
                def.altitudeLayer.AltitudeFor(),
                (minZ + maxZ) * 0.5f);
            pointA = NormalizePoint(geometry.Origin, minX, minZ, width, height);
            pointB = NormalizePoint(geometry.LeftEnd, minX, minZ, width, height);
            pointC = NormalizePoint(geometry.RightEnd, minX, minZ, width, height);
            exactPosition = effectCenter;
#if !RW_1_7
            baseEmission = Graphic.MatSingle.GetFloat(EmissionId);
            baseCoreBlend = Graphic.MatSingle.GetFloat(CoreBlendId);
#endif
        }

        //创建辐射锥 Mote，并将其登记到施法者所在地图的实时绘制系统。
        public static void Spawn(Map map, MunoRadiationConeGeometry geometry, System.Collections.Generic.IEnumerable<Thing> targets)
        {
            Mote_MunoRadiationConeEffect effect =
                (Mote_MunoRadiationConeEffect)ThingMaker.MakeThing(MunoDefDataRef.Mote_MunoRadiationConeEffect);
            effect.Initialize(geometry);
            GenSpawn.Spawn(effect, geometry.Origin.ToIntVec3(), map);
        }

#if RW_1_7
        //特效存在期间登记本模组的 Bloom 层。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            MunoRadiationBloom.Register(this);
        }

        //特效消散后归还 Bloom 层，不影响同时播放的其他辐射。
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            MunoRadiationBloom.Unregister(this);
        }

        //地图移除时释放尚未播放完的辐射。
        public override void Notify_MyMapRemoved()
        {
            base.Notify_MyMapRemoved();
            MunoRadiationBloom.Unregister(this);
        }
#endif

        //按每次发射的独立时间播放能流，保留旧版资源的几何展开方式。
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Find.UIRoot.HideMotes)
            {
                return;
            }

            float age = AgeSecs;
            float progress = Mathf.Clamp01(age / def.mote.Lifespan);
#if RW_1_7
            PropertyBlock.Clear();
            PropertyBlock.SetFloat(ProgressId, progress);
            PropertyBlock.SetFloat(EffectAgeId, age);
            PropertyBlock.SetVector(PointAId, pointA);
            PropertyBlock.SetVector(PointBId, pointB);
            PropertyBlock.SetVector(PointCId, pointC);
#else
            float spread = Mathf.SmoothStep(0.12f, 1f, Mathf.Clamp01(age / 0.4f));
            float pulse = Mathf.Sin(age * 11f);
            Vector4 endCenter = (pointB + pointC) * 0.5f;
            float width = 0.97f + 0.03f * pulse;
            Vector4 animatedB = Vector4.Lerp(pointA, Vector4.Lerp(endCenter, pointB, width), spread);
            Vector4 animatedC = Vector4.Lerp(pointA, Vector4.Lerp(endCenter, pointC, width), spread);
            //缩短最初的暗色阶段，给明亮主体留出播放时间。
            float shaderProgress = progress < 0.2f
                ? Mathf.Lerp(0f, 0.42f, progress / 0.2f)
                : progress < 0.65f
                    ? Mathf.Lerp(0.42f, 0.65f, (progress - 0.2f) / 0.45f)
                    : progress;
            PropertyBlock.Clear();
            PropertyBlock.SetFloat(ProgressId, shaderProgress);
            PropertyBlock.SetFloat(EmissionId, baseEmission * (0.85f + 0.15f * pulse));
            PropertyBlock.SetFloat(CoreBlendId, Mathf.Clamp01(baseCoreBlend + 0.06f * Mathf.Sin(age * 8f)));
            PropertyBlock.SetVector(PointAId, pointA);
            PropertyBlock.SetVector(PointBId, animatedB);
            PropertyBlock.SetVector(PointCId, animatedC);
#endif

            Matrix4x4 matrix = Matrix4x4.TRS(
                effectCenter,
                Quaternion.identity,
                new Vector3(effectSize.x, 1f, effectSize.y));
#if RW_1_7
            const int drawLayer = MunoRadiationBloom.Layer;
#else
            const int drawLayer = 0;
#endif
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, drawLayer, null, 0, PropertyBlock);
        }

        //把世界平面上的坐标换算为包围面片中的零到一 UV 坐标。
        private static Vector4 NormalizePoint(Vector3 point, float minX, float minZ, float width, float height)
        {
            return new Vector4((point.x - minX) / width, (point.z - minZ) / height, 0f, 0f);
        }
    }
}
#endif
