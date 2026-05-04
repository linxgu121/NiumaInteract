using NiumaInteract.Core.Data;
using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace NiumaInteract.Test
{
    /// <summary>
    /// 解密开始交互测试目标。
    /// 用于验证远距离瞄准命中后，通过交互触发解密 UI、机关动画或测试日志。
    /// </summary>
    public sealed class PuzzleStartInteractable : MonoBehaviour, IInteractable
    {
        [Header("显示")]
        [Tooltip("交互 ID，默认为物体名称。可用于任务、存档或调试日志。")]
        [SerializeField] private string interactionId;
        [Tooltip("交互目标显示名称，例如“控制台”“解密机关”。")]
        [SerializeField] private string displayName = "解密机关";
        [Tooltip("交互提示文本，例如“开始解密”。")]
        [SerializeField] private string promptText = "开始解密";
        [Tooltip("交互提示类型。远距离瞄准解密通常可使用屏幕空间或世界空间。")]
        [SerializeField] private PromptType promptType = PromptType.ScreenSpace;
        [Tooltip("世界空间提示挂点。为空时使用 InteractionTransform。")]
        [SerializeField] private Transform promptAnchor;

        [Header("交互规则")]
        [Tooltip("交互检测使用的稳定位置源。为空时使用当前物体 Transform。")]
        [SerializeField] private Transform interactionTransform;
        [Tooltip("交互排序优先级。远距离瞄准机关可以略高于普通拾取物。")]
        [SerializeField] private float priority = 2f;
        [Tooltip("长按触发阈值，单位秒。需要长按进入解密时设置大于 0。")]
        [SerializeField] private float longPressDuration;
        [Tooltip("该目标支持的交互类型。普通解密入口使用 Short；需要蓄力确认时使用 Long。")]
        [SerializeField] private InteractKind supportedKinds = InteractKind.Short;
        [Tooltip("是否只允许触发一次。一次性解密入口开启，反复测试可关闭。")]
        [SerializeField] private bool triggerOnce = true;

        [Header("事件")]
        [Tooltip("交互成功后触发。可绑定打开解密 UI、切换状态、播放机关动画或输出测试日志。")]
        [SerializeField] private UnityEvent onPuzzleStarted;

        private bool _triggered;

        public string InteractionId => string.IsNullOrEmpty(interactionId) ? gameObject.name : interactionId;
        public Transform InteractionTransform => interactionTransform != null ? interactionTransform : transform;
        public string DisplayName => displayName;
        public string PromptText => promptText;
        public PromptType PromptType => promptType;
        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : InteractionTransform;
        public float Priority => priority;
        public float LongPressDuration => longPressDuration;
        public InteractKind SupportedKinds => supportedKinds;

        /// <summary>
        /// 解密入口在未触发或允许重复触发时可交互。
        /// 是否被瞄准、是否在距离内由检测器和仲裁器负责。
        /// </summary>
        public bool CanInteract(in InteractionContext context)
        {
            return isActiveAndEnabled && (!triggerOnce || !_triggered);
        }

        /// <summary>
        /// 执行解密开始事件。
        /// 具体解密逻辑由 UnityEvent 外部绑定，交互目标本身只负责转发触发。
        /// </summary>
        public void Interact(in InteractionRequest request)
        {
            if (!CanInteract(request.Context))
                return;

            if ((supportedKinds & request.Kind) != request.Kind)
                return;

            _triggered = true;
            onPuzzleStarted?.Invoke();
        }

        private void OnValidate()
        {
            priority = priority > 0f ? priority : 0f;
            longPressDuration = longPressDuration > 0f ? longPressDuration : 0f;

            if (supportedKinds == InteractKind.None)
                supportedKinds = InteractKind.Short;
        }
    }
}
