using NiumaInteract.Core.Data;
using NiumaInteract.Core.Interface;
using NiumaUI.Toolkit;
using NiumaUI.Views.Interaction;
using UnityEngine;

namespace NiumaInteract.ToolkitBridge
{
    /// <summary>
    /// UI Toolkit 交互提示接收器。
    /// 挂在 UIRoot/UIBridges 或交互 UI 根物体上，并把它拖给 NiumaInteract 的 Prompt Sink 字段。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptToolkitSink : MonoBehaviour, IInteractionPromptSink
    {
        [Header("Toolkit UI")]
        [Tooltip("UI Toolkit 根控制器。拖核心场景 UIRoot/UIManager 上的 UIToolkitUIManager；为空时可自动查找。")]
        [SerializeField] private UIToolkitUIManager uiManager;

        [Tooltip("未绑定 UIManager 时是否自动查找场景中的 UIToolkitUIManager。正式场景建议手动绑定。")]
        [SerializeField] private bool autoFindUIManager = true;

        [Tooltip("交互提示 ViewId。需要在 UIToolkitViewRegistrySO 中注册，默认 InteractionPrompt。")]
        [SerializeField] private string viewId = "InteractionPrompt";

        [Header("文本")]
        [Tooltip("显示给玩家看的交互按键名称，只影响 UI 文本，不影响真实输入绑定。")]
        [SerializeField] private string interactKeyLabel = "E";

        [Tooltip("提示文本格式。{0}=按键名称，{1}=提示文本，{2}=目标名称。")]
        [SerializeField] private string textFormat = "[{0}] {1} {2}";

        [Header("行为")]
        [Tooltip("没有交互目标时是否关闭 Toolkit View。")]
        [SerializeField] private bool closeViewWhenEmpty = true;

        [Header("调试")]
        [Tooltip("缺少 UIManager 或 ViewId 时是否输出警告。")]
        [SerializeField] private bool logWarnings = true;

        public void ShowPrompt(in InteractionPromptData data)
        {
            if (!data.HasTarget)
            {
                HidePrompt();
                return;
            }

            if (!EnsureUIManager())
                return;

            var displayText = string.Format(
                textFormat,
                interactKeyLabel,
                data.PromptText,
                data.TargetName);

            var viewData = new InteractionPromptToolkitViewData
            {
                HasTarget = data.HasTarget,
                TargetName = data.TargetName,
                PromptText = data.PromptText,
                DisplayText = displayText,
                IsHolding = data.IsHolding,
                HoldProgress = data.HoldProgress
            };

            uiManager.OpenView(ResolveViewId(), viewData);
        }

        public void HidePrompt()
        {
            if (!closeViewWhenEmpty || !EnsureUIManager(false))
                return;

            uiManager.CloseView(ResolveViewId());
        }

        private bool EnsureUIManager(bool logMissing = true)
        {
            if (uiManager != null)
                return true;

            if (autoFindUIManager)
                uiManager = FindAnyObjectByType<UIToolkitUIManager>();

            if (uiManager == null && logMissing)
                Warn("未绑定 UIToolkitUIManager，无法显示交互提示。请拖核心场景 UIRoot/UIManager 上的 UIToolkitUIManager。");

            return uiManager != null;
        }

        private string ResolveViewId()
        {
            return string.IsNullOrWhiteSpace(viewId) ? "InteractionPrompt" : viewId.Trim();
        }

        private void Warn(string message)
        {
            if (logWarnings && !string.IsNullOrWhiteSpace(message))
                Debug.LogWarning($"[InteractionPromptToolkitSink] {message}", this);
        }
    }
}
