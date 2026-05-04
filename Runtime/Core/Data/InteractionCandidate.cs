using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Interface;

namespace NiumaInteract.Core.Data
{
    /// <summary>
    /// 检测层输出的候选目标数据。
    /// 候选只保存检测事实，不保存目标自身配置，也不决定最终焦点。
    /// </summary>
    public readonly struct InteractionCandidate
    {
        /// <summary>
        /// 被检测到的可交互目标。
        /// 目标位置统一从 Target.InteractionTransform 获取，避免重复缓存 Transform 后出现不一致。
        /// </summary>
        public readonly IInteractable Target;

        /// <summary>
        /// 检测时玩家与目标之间的距离。
        /// </summary>
        public readonly float Distance;

        /// <summary>
        /// 玩家朝向与目标方向的点积，范围为 [-1, 1]。
        /// 1 表示目标在正前方，0 表示目标在侧面，负数表示目标在身后。
        /// </summary>
        public readonly float FacingDot;

        /// <summary>
        /// 产生该候选的检测方式，用枚举避免每帧字符串分配。
        /// </summary>
        public readonly DetectMode SourceMode;

        public bool IsValid => Target != null && Target.InteractionTransform != null;

        public InteractionCandidate(
            IInteractable target,
            float distance,
            float facingDot,
            DetectMode sourceMode)
        {
            Target = target;
            Distance = distance;
            FacingDot = facingDot;
            SourceMode = sourceMode;
        }
    }
}
