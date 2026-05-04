using System;

namespace NiumaInteract.Core.Enum
{
    /// <summary>
    /// 目标检测模式
    /// 由 InteractionDetector 组合使用
    /// 使用位掩码支持同时启用多种检测器
    /// </summary>
    [Flags]
    public enum DetectMode
    {
        /// <summary>不启用任何检测器</summary>
        None = 0,
        
        /// <summary>球形范围检测：近距离 NPC、可拾取物体</summary>
        Sphere = 1 << 0,

        /// <summary>射线检测：远距离瞄准、射箭、解密</summary>
        Raycast = 1 << 1,

        /// <summary>启用所有基础检测器</summary>
        All = Sphere | Raycast
    }
}
