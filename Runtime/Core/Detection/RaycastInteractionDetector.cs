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

        [Tooltip("射线命中缓存数量。远距离瞄准会按距离扫描多个命中点，用于跳过误放在交互层但没有交互脚本的碰撞体。")]
        [SerializeField] private int hitBufferSize = 16;

        [Tooltip("编辑器调试射线使用的相机。为空时，运行中使用最近一次检测传入的 ViewCamera，未运行时使用本组件朝向。")]
        [SerializeField] private Camera debugViewCamera;

        private RaycastHit[] _hitBuffer;
        private Vector3 _lastRayOrigin;
        private Vector3 _lastRayDirection;
        private bool _hasLastRay;

        public bool IsEnabled => isEnabled && maxDistance > 0f;

        private void OnValidate()
        {
            maxDistance = maxDistance > 0f ? maxDistance : 0f;
            sphereCastRadius = sphereCastRadius > 0f ? sphereCastRadius : 0f;
            hitBufferSize = hitBufferSize < 1 ? 1 : hitBufferSize;
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
            CacheDebugRay(ray);

            if (!TryFindInteractableHit(ray, context, out var interactable))
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

        private bool TryFindInteractableHit(
            Ray ray,
            in InteractionContext context,
            out IInteractable interactable)
        {
            interactable = null;
            int mask = interactableMask.value | obstructionMask.value;
            if (mask == 0)
                return false;

            EnsureHitBuffer();

            int hitCount;
            if (sphereCastRadius > 0f)
            {
                hitCount = Physics.SphereCastNonAlloc(
                    ray,
                    sphereCastRadius,
                    _hitBuffer,
                    maxDistance,
                    mask,
                    triggerInteraction);
            }
            else
            {
                hitCount = Physics.RaycastNonAlloc(
                    ray,
                    _hitBuffer,
                    maxDistance,
                    mask,
                    triggerInteraction);
            }

            if (hitCount <= 0)
                return false;

            SortHitsByDistance(hitCount);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitBuffer[i];
                if (hit.collider == null)
                    continue;

                int layer = hit.collider.gameObject.layer;
                bool isInteractableLayer = IsLayerInMask(layer, interactableMask);
                bool isObstructionLayer = IsLayerInMask(layer, obstructionMask);

                if (isInteractableLayer)
                {
                    var candidateTarget = hit.collider.GetComponentInParent<IInteractable>();
                    if (candidateTarget != null &&
                        candidateTarget.InteractionTransform != null &&
                        candidateTarget.CanInteract(context))
                    {
                        interactable = candidateTarget;
                        return true;
                    }

                    // 误放在交互层但没有交互脚本的碰撞体不应该挡住后方目标。
                    if (!isObstructionLayer)
                        continue;
                }

                if (isObstructionLayer)
                    return false;
            }

            return false;
        }

        private void EnsureHitBuffer()
        {
            if (_hitBuffer == null || _hitBuffer.Length != hitBufferSize)
                _hitBuffer = new RaycastHit[hitBufferSize];
        }

        private void SortHitsByDistance(int hitCount)
        {
            for (int i = 1; i < hitCount; i++)
            {
                RaycastHit current = _hitBuffer[i];
                int insertIndex = i - 1;

                while (insertIndex >= 0 && _hitBuffer[insertIndex].distance > current.distance)
                {
                    _hitBuffer[insertIndex + 1] = _hitBuffer[insertIndex];
                    insertIndex--;
                }

                _hitBuffer[insertIndex + 1] = current;
            }
        }

        private void CacheDebugRay(Ray ray)
        {
            _lastRayOrigin = ray.origin;
            _lastRayDirection = ray.direction;
            _hasLastRay = true;
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
            GetDebugRay(out var origin, out var direction);

            Gizmos.DrawLine(origin, origin + direction * maxDistance);

            if (sphereCastRadius > 0f)
                Gizmos.DrawWireSphere(origin + direction * maxDistance, sphereCastRadius);
        }

        private void GetDebugRay(out Vector3 origin, out Vector3 direction)
        {
            if (debugViewCamera != null)
            {
                origin = debugViewCamera.transform.position;
                direction = debugViewCamera.transform.forward;
                return;
            }

            if (Application.isPlaying && _hasLastRay)
            {
                origin = _lastRayOrigin;
                direction = _lastRayDirection;
                return;
            }

            origin = transform.position;
            direction = transform.forward;
        }
#endif
    }
}
