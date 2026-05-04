namespace NiumaInteract.Core.Data
{
    /// <summary>
    /// 交互提示显示数据。
    /// 该数据只面向 UI 表现层，不参与检测、评分和交互裁决。
    /// </summary>
    public readonly struct InteractionPromptData
    {
        /// <summary>
        /// 当前是否存在可显示的交互目标。
        /// </summary>
        public readonly bool HasTarget;

        /// <summary>
        /// 目标显示名称。
        /// </summary>
        public readonly string TargetName;

        /// <summary>
        /// 交互提示文本，例如“拾取”“交谈”。
        /// </summary>
        public readonly string PromptText;

        /// <summary>
        /// 当前是否正在按住交互键。
        /// </summary>
        public readonly bool IsHolding;

        /// <summary>
        /// 长按显示进度，范围为 [0, 1]。
        /// 短按目标或无长按阈值时为 0。
        /// </summary>
        public readonly float HoldProgress;

        public InteractionPromptData(
            bool hasTarget,
            string targetName,
            string promptText,
            bool isHolding,
            float holdProgress)
        {
            HasTarget = hasTarget;
            TargetName = targetName;
            PromptText = promptText;
            IsHolding = isHolding;
            HoldProgress = holdProgress;
        }

        /// <summary>
        /// 空提示数据，用于隐藏 UI。
        /// </summary>
        public static InteractionPromptData Empty { get; } =
            new InteractionPromptData(false, string.Empty, string.Empty, false, 0f);
    }
}
