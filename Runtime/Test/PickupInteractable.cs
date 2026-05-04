using NiumaInteract.Core.Data;
using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Interface;
using UnityEngine;

namespace NiumaInteract.Test
{
    /// <summary>
    /// 拾取交互测试物体。
    /// 用于验证：靠近显示 UI，触发拾取后隐藏物体并让 UI 消失。
    /// </summary>
    public sealed class PickupInteractable : MonoBehaviour, IInteractable
    {
        [Header("显示")]
        [Tooltip("交互 ID，默认为物体名称。")]
        [SerializeField] private string interactionId;
        [Tooltip("交互显示名称，默认为“物品”。")]
        [SerializeField] private string displayName = "物品";
        [Tooltip("交互提示文本，默认为“拾取”。")]
        [SerializeField] private string promptText = "拾取";
        [Tooltip("交互提示类型，默认为“世界空间”。")]
        [SerializeField] private PromptType promptType = PromptType.WorldSpace;
        [Tooltip("世界空间提示挂点。为空时使用 InteractionTransform；通常可绑到物体头顶或物品上方。")]
        [SerializeField] private Transform promptAnchor;

        [Header("交互")]
        [Tooltip("交互检测使用的稳定位置源。为空时使用当前物体 Transform。")]
        [SerializeField] private Transform interactionTransform;
        [Tooltip("交互排序优先级。数值越大越容易成为当前焦点目标。")]
        [SerializeField] private float priority = 1f;
        [Tooltip("长按触发阈值，单位秒。短按拾取保持 0 即可。")]
        [SerializeField] private float longPressDuration;
        [Tooltip("该物体支持的交互类型。普通拾取使用 Short；需要长按拾取时选择 Long。")]
        [SerializeField] private InteractKind supportedKinds = InteractKind.Short;

        [Header("拾取结果")]
        [Tooltip("拾取成功后是否直接禁用整个 GameObject。普通一次性拾取物保持开启。")]
        [SerializeField] private bool deactivateGameObjectOnPickup = true;
        [Tooltip("拾取成功后需要隐藏的表现根节点。为空时不额外处理表现节点。")]
        [SerializeField] private GameObject visualRoot;
        [Tooltip("拾取成功后需要禁用的碰撞体。为空时默认禁用当前物体上的 Collider。")]
        [SerializeField] private Collider[] collidersToDisable;

        private bool _picked;

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
        /// 测试物体未被拾取时才允许交互。
        /// 距离、朝向、输入类型不在这里判断。
        /// </summary>
        public bool CanInteract(in InteractionContext context)
        {
            return isActiveAndEnabled && !_picked;
        }

        /// <summary>
        /// 执行拾取结果。
        /// 测试阶段只隐藏物体；正式背包、任务、音效等逻辑后续可以在这里接入。
        /// </summary>
        public void Interact(in InteractionRequest request)
        {
            if (_picked || (supportedKinds & request.Kind) != request.Kind)
                return;

            _picked = true;
            HideAfterPickup();
        }

        private void HideAfterPickup()
        {
            DisableConfiguredColliders();

            if (visualRoot != null)
                visualRoot.SetActive(false);

            if (deactivateGameObjectOnPickup)
                gameObject.SetActive(false);
        }

        private void DisableConfiguredColliders()
        {
            if (collidersToDisable != null && collidersToDisable.Length > 0)
            {
                for (int i = 0; i < collidersToDisable.Length; i++)
                {
                    if (collidersToDisable[i] != null)
                        collidersToDisable[i].enabled = false;
                }

                return;
            }

            var ownCollider = GetComponent<Collider>();
            if (ownCollider != null)
                ownCollider.enabled = false;
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
