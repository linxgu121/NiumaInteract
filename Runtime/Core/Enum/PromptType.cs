namespace NiumaInteract.Core.Enum
{
    /// <summary>
    /// 交互提示显示位置
    /// 决定 UI 渲染方式
    /// </summary>
    public enum PromptType
    {
        /// <summary>不显示交互提示</summary>
        None = 0,

        /// <summary>世界空间：跟随目标物体（如 NPC 头顶）</summary>
        WorldSpace = 1,
        
        /// <summary>屏幕空间：固定在屏幕指定位置（如拾取提示）</summary>
        ScreenSpace = 2
    }
}
