using NiumaInteract.Core.Data;
using NiumaInteract.Core.Enum;
using UnityEngine;

namespace NiumaInteract.Core.Interface
{
    /// <summary>
    /// 可交互目标协议。
    /// 场景物体通过实现该接口向 NiumaInteract 声明自己可以被检测、提示和触发。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 交互目标唯一或半唯一标识，用于调试、任务、存档或日志。
        /// </summary>
        string InteractionId { get; }

        /// <summary>
        /// 交互目标的稳定位置源。
        /// 检测层用它进行距离、朝向和排序计算，不使用 UI 提示挂点作为检测位置。
        /// </summary>
        Transform InteractionTransform { get; }

        /// <summary>
        /// 目标显示名称，例如 NPC 名称、道具名称、机关名称。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 交互提示文本，例如“交谈”“拾取”“长按开启”。
        /// </summary>
        string PromptText { get; }

        /// <summary>
        /// 提示显示类型，由 UI 桥接层决定如何呈现。
        /// </summary>
        PromptType PromptType { get; }

        /// <summary>
        /// 世界空间提示的挂点。为空时可回退到 InteractionTransform。
        /// </summary>
        Transform PromptAnchor { get; }

        /// <summary>
        /// 交互排序优先级。数值越大，越容易成为焦点目标；使用 float 便于评分制细粒度加权。
        /// </summary>
        float Priority { get; }

        /// <summary>
        /// 长按触发阈值。目标不支持长按时，该值可为 0。
        /// </summary>
        float LongPressDuration { get; }

        /// <summary>
        /// 目标支持的交互触发类型，例如短按、长按，或两者都支持。
        /// </summary>
        InteractKind SupportedKinds { get; }

        /// <summary>
        /// 目标在当前上下文下是否允许交互。
        /// </summary>
        bool CanInteract(in InteractionContext context);

        /// <summary>
        /// 执行一次已经由仲裁器确认过的交互请求。
        /// 目标只执行业务逻辑，不返回仲裁结果。
        /// </summary>
        void Interact(in InteractionRequest request);
    }
}
