namespace NiumaInteract.Core.Enum
{
    /// <summary>
    /// 交互失败原因。
    /// 使用枚举便于 UI、日志、任务系统分流处理，也避免依赖字符串匹配。
    /// </summary>
    public enum InteractionFailReason
    {
        /// <summary>没有失败，通常用于成功结果。</summary>
        None = 0,

        /// <summary>当前没有焦点目标。</summary>
        NoTarget = 1,

        /// <summary>外部门禁拒绝交互，例如对话、菜单、电影镜头期间。</summary>
        GateBlocked = 2,

        /// <summary>目标自身在当前上下文下不允许交互。</summary>
        TargetRejected = 3,

        /// <summary>目标不支持本次裁决出的交互类型。</summary>
        UnsupportedKind = 4,

        /// <summary>交互触发冷却中。</summary>
        Cooldown = 5,

        /// <summary>交互上下文无效，例如缺少玩家对象或视角相机。</summary>
        InvalidContext = 6
    }
}
