using System.Collections.Generic;
using NiumaInteract.Core.Data;
using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Interface;
using UnityEngine;

namespace NiumaInteract.Core.Detection
{
    /// <summary>
    /// 射线交互检测器。
    /// 适合远距离瞄准、准星机关、解密台、远处按钮等需要视线命中的交互目标。
    /// </summary>
    public sealed class RaycastInteractionDetector : MonoBehaviour, IInteractionDetector
    {
        [Tooltip("是否启用该射线检测器。关闭后不会输出远距离瞄准候选目标。")]
        [SerializeField] private bool isEnabled = true;

        [Tooltip("可被远距离瞄准命中的交互目标层。远处机关、解密物体、按钮等 Collider 应放在这些 Layer。")]
        [SerializeField] private LayerMask interactableMask;

        [Tooltip("会阻挡远距离交互视线的遮挡层，例如墙体、地形、门板。为空时不会检查遮挡。")]
        [SerializeField] private LayerMask obstructionMask;

        [Tooltip("最大瞄准检测距离，单位为 Unity 世界单位。")]
        [SerializeField] private float maxDistance = 12f;

        [Tooltip("瞄准容错半径。为 0 时使用 Raycast；大于 0 时使用 SphereCast，适合手柄或小目标解密机关。")]
        [SerializeField] private float sphereCastRadius;

        [Tooltip("是否检测 Trigger Collider。解密触发区通常可使用 Collide。")]
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Tooltip("命中同一目标时是否合并到已有候选中。开启后 Raycast 与 Sphere 检测命中同一目标会合并 SourceMode。")]
        [SerializeField] private bool mergeWithExistingCandidate = true;

        public bool IsEnabled => isEnabled && maxDistance > 0f;

        private void OnValidate()
        {
            maxDistance = maxDistance > 0f ? maxDistance : 0f;
            sphereCastRadius = sphereCastRadius > 0f ? sphereCastRadius : 0f;
        }

        /// <summary>
        /// 从当前视角相机中心向前检测远距离交互目标。
        /// 检测器只输出命中事实，不决定最终焦点，也不触发目标。
        /// </summary>
        public void Collect(in InteractionContext context, List<InteractionCandidate> results)
        {
            if (!IsEnabled || !context.IsValid || results == null)
                return;

            var viewCamera = context.ViewCamera;
            if (viewCamera == null)
                return;

            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            if (!TryHit(ray, out var hit))
                return;

            if (!IsLayerInMask(hit.collider.gameObject.layer, interactableMask))
                return;

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null || interactable.InteractionTransform == null)
                return;

            if (!interactable.CanInteract(context))
                return;

            var actorTransform = context.ActorTransform;
            if (actorTransform != null && interactable.InteractionTransform == actorTransform)
                return;

            Vector3 origin = actorTransform != null ? actorTransform.position : viewCamera.transform.position;
            Vector3 targetPosition = interactable.InteractionTransform.position;
            Vector3 toTarget = targetPosition - origin;
            float distance = toTarget.magnitude;

            var candidate = new InteractionCandidate(
                interactable,
                distance,
                1f,
                DetectMode.Raycast);

            if (mergeWithExistingCandidate && TryMergeCandidate(results, candidate))
                return;

            results.Add(candidate);
        }

        private bool TryHit(Ray ray, out RaycastHit hit)
        {
            int mask = interactableMask.value | obstructionMask.value;
            if (mask == 0)
            {
                hit = default;
                return false;
            }

            if (sphereCastRadius > 0f)
            {
                return Physics.SphereCast(
                    ray,
                    sphereCastRadius,
                    out hit,
                    maxDistance,
                    mask,
                    triggerInteraction);
            }

            return Physics.Raycast(
                ray,
                out hit,
                maxDistance,
                mask,
                triggerInteraction);
        }

        private static bool TryMergeCandidate(
            List<InteractionCandidate> results,
            in InteractionCandidate raycastCandidate)
        {
            for (int i = 0; i < results.Count; i++)
            {
                var current = results[i];
                if (current.Target != raycastCandidate.Target)
                    continue;

                results[i] = new InteractionCandidate(
                    current.Target,
                    current.Distance < raycastCandidate.Distance ? current.Distance : raycastCandidate.Distance,
                    current.FacingDot > raycastCandidate.FacingDot ? current.FacingDot : raycastCandidate.FacingDot,
                    current.SourceMode | DetectMode.Raycast);

                return true;
            }

            return false;
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.78f, 0.2f, 0.45f);
            var origin = transform.position;
            var direction = transform.forward;

            Gizmos.DrawLine(origin, origin + direction * maxDistance);

            if (sphereCastRadius > 0f)
                Gizmos.DrawWireSphere(origin + direction * maxDistance, sphereCastRadius);
        }
#endif
    }
}
