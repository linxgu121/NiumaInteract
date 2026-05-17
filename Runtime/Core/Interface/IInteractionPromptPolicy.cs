namespace NiumaInteract.Core.Interface
{
    /// <summary>
    /// 交互提示显示策略。
    /// 可交互目标可以选择实现该接口，告诉提示桥接层一次成功交互后是否需要继续压制当前提示。
    /// </summary>
    public interface IInteractionPromptPolicy
    {
        /// <summary>
        /// 交互成功后是否隐藏并压制当前目标提示。
        /// 一次性拾取、开门后消失等目标通常返回 true；部分拾取后仍可继续拾取的目标可以返回 false。
        /// </summary>
        bool SuppressPromptAfterSuccess { get; }
    }
}
