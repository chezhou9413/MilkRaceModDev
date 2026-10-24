using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp;

//发射时建立定向束带和命中反馈，使用真实墙面约束特效长度。
internal static class MunoRadiationBurstGeometry
{
    private static readonly int[] BeamIndices = { 1, 4, 6, 8, 10, 12, 15 };
    private const int RayCount = 17;

    //一次性采样场景，帧绘制不再搜索地图或分配粒子对象。
    internal static Mesh Build(Map map, MunoRadiationConeGeometry geometry, IEnumerable<Thing> targets, int seed)
    {
        MunoRadiationMeshWriter writer = new();
        System.Random random = new(seed);
        Vector3[] rayEnds = new Vector3[RayCount];
        bool[] blocked = new bool[RayCount];
        float beamAltitude = AltitudeLayer.MoteOverheadLow.AltitudeFor();
        float groundAltitude = AltitudeLayer.MoteLow.AltitudeFor();
        for (int index = 0; index < RayCount; index++)
        {
            float angle = Mathf.Lerp(-geometry.HalfAngleDegrees, geometry.HalfAngleDegrees, index / (float)(RayCount - 1));
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * geometry.Direction;
            float length = VisibleLength(map, geometry.Origin, direction, geometry.Range, out blocked[index]);
            rayEnds[index] = direction * length;
        }

        Vector3 muzzle = geometry.Direction * Mathf.Min(0.72f, rayEnds[8].magnitude * 0.4f);
        muzzle.y = beamAltitude;
        writer.Wave(rayEnds, geometry.Range, groundAltitude, geometry.HalfAngleDegrees, geometry.Direction);
        foreach (int index in BeamIndices)
        {
            Vector3 end = rayEnds[index];
            end.y = beamAltitude;
            bool central = index == 8;
            float weight = central ? 1f : 0.28f + (1f - Mathf.Abs(index - 8) / 8f) * 0.17f;
            writer.Ribbon(muzzle, end, central ? 0.82f : 0.42f, weight,
                central ? 0f : 0.012f + Next(random) * 0.02f,
                central ? 0.48f : 0.31f + Next(random) * 0.11f, Next(random) * 31f);
            if (blocked[index])
                Impact(writer, end, geometry.Direction, rayEnds[index].magnitude / 220f, 0.6f, random);
        }

        writer.Flash(muzzle, 1.65f, 1f, 1.05f, 0f, 0.2f, Next(random) * 31f);
        writer.Flash(muzzle, 1.25f, 2f, 0.2f, 0.01f, 0.24f, Next(random) * 31f);
        Sparks(writer, muzzle, geometry.Direction, 11, 0.025f, 0.5f, random);

        int impactCount = 0;
        foreach (Thing target in targets)
        {
            if (impactCount >= 16) break;
            Vector3 offset = (target.DrawPos - geometry.Origin).Yto0();
            float delay = offset.magnitude / 220f + Next(random) * 0.025f;
            offset.y = beamAltitude + 0.01f;
            Impact(writer, offset, geometry.Direction, delay, target is Pawn ? 0.8f : 1f, random);
            impactCount++;
        }
        return writer.Finish();
    }

    //命中点先出现白热闪点，再留下破碎圆环和少量定向飞散火花。
    private static void Impact(MunoRadiationMeshWriter writer, Vector3 position, Vector3 forward,
        float delay, float scale, System.Random random)
    {
        float seed = Next(random) * 31f;
        writer.Flash(position, scale * 1.15f, 1f, 0.75f, delay, 0.22f, seed);
        Vector3 ground = position;
        ground.y = AltitudeLayer.MoteLow.AltitudeFor();
        writer.Flash(ground, scale * 1.4f, 2f, 0.24f, delay + 0.025f, 0.43f, seed);
        Sparks(writer, position, forward, 4, delay + 0.025f, scale, random);
    }

    //火花的飞行路径和寿命写入网格，由 Shader 推进亮点及尾迹。
    private static void Sparks(MunoRadiationMeshWriter writer, Vector3 position, Vector3 forward,
        int count, float delay, float scale, System.Random random)
    {
        for (int index = 0; index < count; index++)
        {
            float angle = Mathf.Lerp(-105f, 105f, Next(random));
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Vector3 end = position + direction * (0.7f + Next(random) * 1.8f) * scale;
            writer.Ribbon(position, end, 0.035f + Next(random) * 0.025f, 0.48f,
                delay + Next(random) * 0.07f, 0.18f + Next(random) * 0.24f, Next(random) * 31f, true);
        }
    }

    //按原版视线格遍历找到墙面或地图边缘，避免光束越过阻挡继续显示。
    private static float VisibleLength(Map map, Vector3 origin, Vector3 direction, float range, out bool blocked)
    {
        blocked = false;
        IntVec3 start = origin.ToIntVec3();
        IntVec3 end = (origin + direction * range).ToIntVec3();
        foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(start, end))
        {
            if (cell == start) continue;
            bool inBounds = cell.InBounds(map);
            if (inBounds && cell.CanBeSeenOverFast(map)) continue;
            blocked = inBounds;
            float entryX = Mathf.Abs(direction.x) < 0.0001f ? float.NegativeInfinity
                : ((direction.x > 0f ? cell.x : cell.x + 1f) - origin.x) / direction.x;
            float entryZ = Mathf.Abs(direction.z) < 0.0001f ? float.NegativeInfinity
                : ((direction.z > 0f ? cell.z : cell.z + 1f) - origin.z) / direction.z;
            return Mathf.Clamp(Mathf.Max(entryX, entryZ), 0.1f, range);
        }
        return range;
    }

    //使用独立随机源变化外观，避免视觉粒子消耗游戏随机序列。
    private static float Next(System.Random random)
    {
        return (float)random.NextDouble();
    }
}
