using NiumaInteract.Core.Data;
using NiumaInteract.Core.Interface;
using UnityEngine;

namespace NiumaInteract.Core.Bridge
{
    /// <summary>
    /// 交互提示桥接器。
    /// 监听交互黑板事件，把当前焦点目标和按住进度转换为 UI 可用的数据。
    /// </summary>
    public sealed class InteractionPromptBridge : MonoBehaviour
    {
        [Tooltip("交互模块根控制器。用于读取黑板中的当前目标、按住时间和交互结果。")]
        [SerializeField] private NiumaInteractionController controller;
        [Tooltip("交互提示 UI 输出脚本。简单提示拖 SimpleInteractionPromptSink；正式 UI 拖团队制作的 InteractionPrompt 脚本。")]
        [SerializeField] private MonoBehaviour promptSinkProvider;
        [Tooltip("未手动绑定 Controller 时，是否自动在场景中查找 NiumaInteractionController。")]
        [SerializeField] private bool autoFindController = true;
        [Tooltip("交互成功后是否立即隐藏提示。拾取、开门等一次性交互通常保持开启。")]
        [SerializeField] private bool hideOnSuccess = true;

        private IInteractionPromptSink _promptSink;
        private InteractionBlackboard _boundBlackboard;
        private IInteractable _suppressedTarget;
        private float _lastHoldTime;
        private bool _isBound;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            Bind();
            RefreshPrompt();
        }

        private void OnDisable()
        {
            HidePrompt();
            Unbind();
        }

        /// <summary>
        /// 重新绑定控制器和 UI 输出端。
        /// 当场景运行时替换 Controller 或 PromptSink 时调用。
        /// </summary>
        public void Rebind(NiumaInteractionController newController, IInteractionPromptSink newPromptSink)
        {
            Unbind();

            controller = newController;
            promptSinkProvider = newPromptSink as MonoBehaviour;
            _promptSink = newPromptSink;

            Bind();
            RefreshPrompt();
        }

        private void ResolveReferences()
        {
            if (controller == null && autoFindController)
                controller = FindObjectOfType<NiumaInteractionController>();

            _promptSink = promptSinkProvider as IInteractionPromptSink;

            if (promptSinkProvider != null && _promptSink == null)
            {
                Debug.LogWarning(
                    $"{nameof(InteractionPromptBridge)} 的 Prompt Sink Provider 没有实现 {nameof(IInteractionPromptSink)}。",
                    this);
            }
        }

        private void Bind()
        {
            ResolveReferences();

            if (controller == null || _promptSink == null || _isBound)
                return;

            _boundBlackboard = controller.Blackboard;
            _boundBlackboard.OnTargetChanged += OnTargetChanged;
            _boundBlackboard.OnHoldTimeChanged += OnHoldTimeChanged;
            _boundBlackboard.OnInteractionResult += OnInteractionResult;
            _isBound = true;
        }

        private void Unbind()
        {
            if (!_isBound || _boundBlackboard == null)
                return;

            _boundBlackboard.OnTargetChanged -= OnTargetChanged;
            _boundBlackboard.OnHoldTimeChanged -= OnHoldTimeChanged;
            _boundBlackboard.OnInteractionResult -= OnInteractionResult;
            _boundBlackboard = null;
            _isBound = false;
        }

        private void OnTargetChanged(IInteractable target)
        {
            _lastHoldTime = 0f;

            if (!ReferenceEquals(target, _suppressedTarget))
                _suppressedTarget = null;

            RefreshPrompt();
        }

        private void OnHoldTimeChanged(float holdTime)
        {
            _lastHoldTime = holdTime;
            RefreshPrompt();
        }

        private void OnInteractionResult(InteractionResult result)
        {
            if (!hideOnSuccess || !result.Succeeded)
                return;

            if (result.Target is IInteractionPromptPolicy promptPolicy && !promptPolicy.SuppressPromptAfterSuccess)
            {
                _suppressedTarget = null;
                RefreshPrompt();
                return;
            }

            _suppressedTarget = result.Target;
            HidePrompt();
        }

        private void RefreshPrompt()
        {
            if (_promptSink == null || promptSinkProvider == null || controller == null)
                return;

            var target = controller.Blackboard.CurrentTarget;
            if (target == null)
            {
                HidePrompt();
                return;
            }

            if (ReferenceEquals(target, _suppressedTarget))
            {
                HidePrompt();
                return;
            }

            var data = new InteractionPromptData(
                true,
                target.DisplayName,
                target.PromptText,
                _lastHoldTime > 0f,
                CalculateHoldProgress(target, _lastHoldTime));

            _promptSink.ShowPrompt(data);
        }

        private void HidePrompt()
        {
            if (promptSinkProvider != null)
                _promptSink?.HidePrompt();
        }

        private static float CalculateHoldProgress(IInteractable target, float holdTime)
        {
            if (target == null || target.LongPressDuration <= 0f || holdTime <= 0f)
                return 0f;

            float progress = holdTime / target.LongPressDuration;
            if (progress <= 0f)
                return 0f;

            return progress >= 1f ? 1f : progress;
        }
    }
}
