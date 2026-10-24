using System.Collections.Generic;
using UnityEngine;

namespace MunoRaceLib.MunoComp;

//把束带、闪光和飞散光点合入一次提交，动画参数随顶点上传。
internal sealed class MunoRadiationMeshWriter
{
    private readonly List<Vector3> vertices = new(1536);
    private readonly List<Vector4> coordinates = new(1536);
    private readonly List<Vector4> timings = new(1536);
    private readonly List<int> triangles = new(4096);

    //建立有轻微弯曲的束带，宽度独立于射程和锥角。
    internal void Ribbon(Vector3 start, Vector3 end, float width, float strength,
        float delay, float duration, float seed, bool spark = false)
    {
        Vector3 forward = (end - start).normalized;
        Vector3 side = new(forward.z, 0f, -forward.x);
        float length = (end - start).magnitude;
        int segments = spark ? 1 : Mathf.Max(2, Mathf.CeilToInt(length / 0.65f));
        Vector4 timing = new(delay, duration, seed, length);
        int first = vertices.Count;
        for (int segment = 0; segment <= segments; segment++)
        {
            float along = segment / (float)segments;
            float bend = spark ? 0f : Mathf.Sin(along * 17f + seed) * Mathf.Sin(along * Mathf.PI) * width * 0.09f;
            Vector3 center = Vector3.Lerp(start, end, along) + side * bend;
            float halfWidth = width * (spark ? 1f : Mathf.Lerp(0.7f, 1f, along));
            AddVertex(center - side * halfWidth, new Vector4(along, -1f, spark ? 3f : 0f, strength), timing);
            AddVertex(center + side * halfWidth, new Vector4(along, 1f, spark ? 3f : 0f, strength), timing);
            if (segment > 0) AddStrip(first + (segment - 1) * 2);
        }
    }

    //用独立面片承载炮口、命中点和不完整的扩散圆环。
    internal void Flash(Vector3 center, float radius, float kind, float strength,
        float delay, float duration, float seed)
    {
        int first = vertices.Count;
        Vector4 timing = new(delay, duration, seed, radius);
        AddVertex(center + new Vector3(-radius, 0f, -radius), new Vector4(-1f, -1f, kind, strength), timing);
        AddVertex(center + new Vector3(radius, 0f, -radius), new Vector4(1f, -1f, kind, strength), timing);
        AddVertex(center + new Vector3(-radius, 0f, radius), new Vector4(-1f, 1f, kind, strength), timing);
        AddVertex(center + new Vector3(radius, 0f, radius), new Vector4(1f, 1f, kind, strength), timing);
        AddStrip(first);
    }

    //弧形压力波只保留向外移动的窄前沿，边界使用逐射线的墙面截断距离。
    internal void Wave(Vector3[] ends, float range, float altitude, float halfAngle, Vector3 forward)
    {
        int first = vertices.Count;
        Vector4 timing = new(0.015f, range / 185f + 0.08f, 0f, range);
        for (int index = 0; index < ends.Length; index++)
        {
            float across = index / (float)(ends.Length - 1) * 2f - 1f;
            Vector3 direction = Quaternion.AngleAxis(across * halfAngle, Vector3.up) * forward;
            Vector3 near = direction * 0.5f;
            Vector3 far = ends[index];
            float fraction = far.magnitude / range;
            near.y = far.y = altitude;
            AddVertex(near, new Vector4(0.5f / range, across, 4f, 0.17f), timing);
            AddVertex(far, new Vector4(fraction, across, 4f, 0.17f), timing);
            if (index > 0) AddStrip(first + (index - 1) * 2);
        }
    }

    //完成静态网格，之后每帧只更新播放时间。
    internal Mesh Finish()
    {
        Mesh mesh = new() { name = "MunoRadiationBurst" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, coordinates);
        mesh.SetUVs(1, timings);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    //保存面片的局部采样坐标、类型、亮度与独立播放时序。
    private void AddVertex(Vector3 position, Vector4 coordinate, Vector4 timing)
    {
        vertices.Add(position);
        coordinates.Add(coordinate);
        timings.Add(timing);
    }

    //连接相邻两对顶点，所有面片共用同一个材质。
    private void AddStrip(int first)
    {
        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 1);
        triangles.Add(first + 1);
        triangles.Add(first + 2);
        triangles.Add(first + 3);
    }
}
