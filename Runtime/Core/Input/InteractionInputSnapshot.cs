namespace NiumaInteract.Core.Input
{
    /// <summary>
    /// 交互输入快照。
    /// 这是输入层对外输出的数据，只描述输入事实，不包含目标阈值和长按进度。
    /// </summary>
    public readonly struct InteractionInputSnapshot
    {
        /// <summary>
        /// 当前帧是否刚按下交互键。
        /// </summary>
        public readonly bool PressedThisFrame;

        /// <summary>
        /// 当前帧是否刚松开交互键。
        /// </summary>
        public readonly bool ReleasedThisFrame;

        /// <summary>
        /// 当前帧交互键是否持续按住。
        /// </summary>
        public readonly bool IsHolding;

        /// <summary>
        /// 当前连续按住时长。长按进度由外部结合 CurrentTarget.LongPressDuration 计算。
        /// </summary>
        public readonly float HoldTime;

        public InteractionInputSnapshot(
            bool pressedThisFrame,
            bool releasedThisFrame,
            bool isHolding,
            float holdTime)
        {
            PressedThisFrame = pressedThisFrame;
            ReleasedThisFrame = releasedThisFrame;
            IsHolding = isHolding;
            HoldTime = holdTime;
        }

        public static InteractionInputSnapshot Empty { get; } =
            new InteractionInputSnapshot(false, false, false, 0f);
    }
}
