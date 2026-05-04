using NiumaInteract.Core.Enum;

namespace NiumaInteract.Core.Arbitration
{
    /// <summary>
    /// 交互评分工具。
    /// 仲裁器只组合权重和候选数据，具体评分曲线统一放在这里，便于后续扩展和调参。
    /// </summary>
    public static class InteractionScoring
    {
        /// <summary>
        /// 计算朝向分。FacingDot 原始范围通常是 [-1, 1]，背后的目标不获得朝向分。
        /// </summary>
        public static float EvaluateFacing(float facingDot)
        {
            return Clamp01(facingDot);
        }

        /// <summary>
        /// 根据距离和曲线类型计算距离分，返回范围保持在 [0, 1]。
        /// </summary>
        public static float EvaluateDistance(
            float distance,
            InteractionDistanceFalloffType falloffType,
            float maxScoreDistance)
        {
            float safeDistance = Max(0f, distance);
            float safeMaxDistance = Max(0.0001f, maxScoreDistance);

            switch (falloffType)
            {
                case InteractionDistanceFalloffType.Linear:
                    return Clamp01(1f - safeDistance / safeMaxDistance);

                case InteractionDistanceFalloffType.Binary:
                    return safeDistance <= safeMaxDistance ? 1f : 0f;

                case InteractionDistanceFalloffType.Inverse:
                default:
                    return 1f / (1f + safeDistance);
            }
        }

        public static float Clamp01(float value)
        {
            if (value <= 0f)
                return 0f;

            return value >= 1f ? 1f : value;
        }

        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}
