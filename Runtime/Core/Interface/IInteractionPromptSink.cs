using NiumaInteract.Core.Data;

namespace NiumaInteract.Core.Interface
{
    /// <summary>
    /// 交互提示输出接口。
    /// UI 模块实现该接口，交互模块只向它推送提示数据，不关心具体 UI 如何绘制。
    /// </summary>
    public interface IInteractionPromptSink
    {
        /// <summary>
        /// 显示或刷新交互提示。
        /// </summary>
        void ShowPrompt(in InteractionPromptData data);

        /// <summary>
        /// 隐藏交互提示。
        /// </summary>
        void HidePrompt();
    }
}
