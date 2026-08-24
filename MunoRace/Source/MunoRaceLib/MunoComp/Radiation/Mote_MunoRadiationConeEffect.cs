using MunoRaceLib.MunoDefRef;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //绘制短寿命辐射锥平面，并把三个锥形顶点与播放进度逐帧传给 CL Shader。
    public class Mote_MunoRadiationConeEffect : Mote
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int PointAId = Shader.PropertyToID("_PointA");
        private static readonly int PointBId = Shader.PropertyToID("_PointB");
        private static readonly int PointCId = Shader.PropertyToID("_PointC");
        private static readonly MaterialPropertyBlock PropertyBlock = new MaterialPropertyBlock();

        private Vector4 pointA;
        private Vector4 pointB;
        private Vector4 pointC;
        private Vector3 effectCenter;
        private Vector2 effectSize;

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
        }

        //创建辐射锥 Mote，并将其登记到施法者所在地图的实时绘制系统。
        public static void Spawn(Map map, MunoRadiationConeGeometry geometry)
        {
            Mote_MunoRadiationConeEffect effect =
                (Mote_MunoRadiationConeEffect)ThingMaker.MakeThing(MunoDefDataRef.Mote_MunoRadiationConeEffect);
            effect.Initialize(geometry);
            GenSpawn.Spawn(effect, geometry.Origin.ToIntVec3(), map);
        }

        //逐帧驱动 Shader 播放进度，并在世界坐标包围面片上绘制辐射三角形。
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Find.UIRoot.HideMotes)
            {
                return;
            }

            float progress = Mathf.Clamp01(AgeSecs / def.mote.Lifespan);
            PropertyBlock.Clear();
            PropertyBlock.SetFloat(ProgressId, progress);
            PropertyBlock.SetVector(PointAId, pointA);
            PropertyBlock.SetVector(PointBId, pointB);
            PropertyBlock.SetVector(PointCId, pointC);

            Matrix4x4 matrix = Matrix4x4.TRS(
                effectCenter,
                Quaternion.identity,
                new Vector3(effectSize.x, 1f, effectSize.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Graphic.MatSingle, 0, null, 0, PropertyBlock);
        }

        //把世界平面上的坐标换算为包围面片中的零到一 UV 坐标。
        private static Vector4 NormalizePoint(Vector3 point, float minX, float minZ, float width, float height)
        {
            return new Vector4((point.x - minX) / width, (point.z - minZ) / height, 0f, 0f);
        }
    }
}
