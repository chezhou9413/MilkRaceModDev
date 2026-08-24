using UnityEngine;
using Verse;

namespace MunoRaceLib.MunoComp
{
    //集中保存辐射锥的方向、射程和三个世界顶点，供伤害筛选与 Shader 绘制共同使用。
    public struct MunoRadiationConeGeometry
    {
        public readonly bool IsValid;
        public readonly Vector3 Origin;
        public readonly Vector3 LeftEnd;
        public readonly Vector3 RightEnd;
        public readonly Vector3 Direction;
        public readonly float Range;
        public readonly float HalfAngleDegrees;

        //根据已校验的数据建立一份不可变的辐射锥几何信息。
        private MunoRadiationConeGeometry(
            Vector3 origin,
            Vector3 leftEnd,
            Vector3 rightEnd,
            Vector3 direction,
            float range,
            float halfAngleDegrees)
        {
            IsValid = true;
            Origin = origin;
            LeftEnd = leftEnd;
            RightEnd = rightEnd;
            Direction = direction;
            Range = range;
            HalfAngleDegrees = halfAngleDegrees;
        }

        //根据施法者、瞄准点和能力参数计算辐射锥的起点及左右端点。
        public static bool TryCreate(
            Pawn pawn,
            LocalTargetInfo target,
            VerbProperties_MunoRadiationCone props,
            out MunoRadiationConeGeometry geometry)
        {
            geometry = default(MunoRadiationConeGeometry);
            if (pawn?.Map == null || props == null || !target.Cell.IsValid || target.Cell == pawn.Position)
            {
                return false;
            }

            Vector3 origin = pawn.Position.ToVector3Shifted().Yto0();
            Vector3 targetPosition = target.Cell.ToVector3Shifted().Yto0();
            Vector3 targetOffset = targetPosition - origin;
            if (targetOffset.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 direction = targetOffset.normalized;
            float range = Mathf.Max(props.range, 0.1f);
            float halfAngle = Mathf.Clamp(props.coneAngleDegrees, 0.1f, 179f) * 0.5f;
            Vector3 leftDirection = Quaternion.AngleAxis(-halfAngle, Vector3.up) * direction;
            Vector3 rightDirection = Quaternion.AngleAxis(halfAngle, Vector3.up) * direction;
            geometry = new MunoRadiationConeGeometry(
                origin,
                origin + leftDirection * range,
                origin + rightDirection * range,
                direction,
                range,
                halfAngle);
            return true;
        }

        //判断世界坐标是否位于当前辐射锥的射程和夹角范围内。
        public bool Contains(Vector3 worldPosition)
        {
            Vector3 offset = (worldPosition - Origin).Yto0();
            float squareDistance = offset.sqrMagnitude;
            if (!IsValid || squareDistance <= 0.0001f || squareDistance > Range * Range)
            {
                return false;
            }

            float directionDot = Vector3.Dot(Direction, offset / Mathf.Sqrt(squareDistance));
            float minimumDot = Mathf.Cos(HalfAngleDegrees * Mathf.Deg2Rad);
            return directionDot >= minimumDot;
        }
    }
}
