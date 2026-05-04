namespace NiumaInteract.Core.Interface
{
    /// <summary>
    /// 交互门禁接口。
    /// 用于让外部系统决定当前是否允许交互，例如对话、菜单、电影镜头期间可以拒绝交互。
    /// NiumaInteract 只读取该接口，不直接依赖 UIManager 或其它具体模块。
    /// </summary>
    public interface IInteractionGate
    {
        /// <summary>
        /// 当前是否允许触发交互。
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// 拒绝交互时的原因，主要用于调试和日志显示。
        /// </summary>
        string BlockReason { get; }
    }
}
