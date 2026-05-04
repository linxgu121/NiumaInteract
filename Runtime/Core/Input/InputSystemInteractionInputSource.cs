using UnityEngine;
using UnityEngine.InputSystem;

namespace NiumaInteract.Core.Input
{
    /// <summary>
    /// 基于 Unity Input System 的交互输入源。
    /// 只负责读取交互动作是否被按住，不判断短按、长按、目标和交互类型。
    /// </summary>
    public sealed class InputSystemInteractionInputSource : InteractionInputSourceBase
    {
        [Header("输入动作")]
        [Tooltip("交互输入动作引用。通常绑定玩家的 Interact / Use 动作，例如 E 键、手柄 A 键。")]
        [SerializeField] private InputActionReference interactAction;

        [Header("生命周期")]
        [Tooltip("是否由该组件自动启用和禁用输入动作。若动作由 PlayerInput 或其他系统统一管理，则关闭。")]
        [SerializeField] private bool manageActionLifecycle = true;

        /// <summary>
        /// 当前交互动作引用。
        /// 运行时切换输入配置或重绑定后，可以通过该属性替换。
        /// </summary>
        public InputActionReference InteractAction
        {
            get => interactAction;
            set
            {
                if (interactAction == value)
                    return;

                SetActionEnabled(false);
                interactAction = value;
                SetActionEnabled(isActiveAndEnabled);
            }
        }

        private void OnEnable()
        {
            SetActionEnabled(true);
        }

        private void OnDisable()
        {
            SetActionEnabled(false);
        }

        /// <summary>
        /// 读取当前帧交互动作是否处于按住状态。
        /// 输入管线会根据该状态计算 PressedThisFrame、ReleasedThisFrame 和 HoldTime。
        /// </summary>
        public override void FetchRawInput(ref InteractionRawInput rawInput)
        {
            var action = interactAction != null ? interactAction.action : null;
            rawInput.InteractHeld = action != null && action.IsPressed();
        }

        private void SetActionEnabled(bool enabled)
        {
            if (!manageActionLifecycle || interactAction == null || interactAction.action == null)
                return;

            if (enabled)
                interactAction.action.Enable();
            else
                interactAction.action.Disable();
        }
    }
}
