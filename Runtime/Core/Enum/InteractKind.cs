using System;

namespace NiumaInteract.Core.Enum
{
    /// <summary>
    /// 交互触发类型
    /// 由仲裁器根据按住时长与目标声明的阈值裁决
    /// 使用位掩码支持同时声明短按和长按交互
    /// </summary>
    [Flags]
    public enum InteractKind
    {
        /// <summary>不支持任何交互触发</summary>
        None = 0,

        /// <summary>短按交互（松手时未达目标阈值）</summary>
        Short = 1 << 0,
        
        /// <summary>长按交互（松手时达到或超过目标阈值）</summary>
        Long = 1 << 1
    }
}
