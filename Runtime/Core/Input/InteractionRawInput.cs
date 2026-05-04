namespace NiumaInteract.Core.Input
{
    /// <summary>
    /// 交互原始输入。
    /// 只表示当前帧输入源读到的事实，不做短按、长按或目标相关裁决。
    /// </summary>
    public struct InteractionRawInput
    {
        /// <summary>
        /// 当前帧交互键是否处于按下状态。
        /// </summary>
        public bool InteractHeld;

        /// <summary>
        /// 清空原始输入，通常在输入源为空或被阻塞时使用。
        /// </summary>
        public void Clear()
        {
            InteractHeld = false;
        }
    }
}
