using System.Collections.Generic;
using NiumaInteract.Core.Data;
using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Interface;
using UnityEngine;

namespace NiumaInteract.Core.Detection
{
    /// <summary>
    /// 球形范围交互检测器。
    /// 适合 NPC、拾取物、近距离机关等范围交互目标。
    /// </summary>
    public sealed class SphereInteractionDetector : MonoBehaviour, IInteractionDetector
    {
        [Tooltip("是否启用该球形检测器。关闭后不会向候选列表输出目标。")]
        [SerializeField] private bool isEnabled = true;
        [Tooltip("只检测交互目标所在层。不要使用 Everything,避免每帧扫描大量无关 Collider。")]
        [SerializeField] private LayerMask interactableMask;
        [Tooltip("以 Actor 位置为中心的检测半径，单位为 Unity 世界单位。")]
        [SerializeField] private float radius = 2.5f;
        [Tooltip("NonAlloc 检测缓冲区大小。场景内同时进入范围的 Collider 很多时需要调大。")]
        [SerializeField] private int bufferSize = 32;
        [Tooltip("是否检测 Trigger Collider。拾取物或 NPC 范围通常使用 Collide。")]
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        private Collider[] _buffer;
        private readonly HashSet<IInteractable> _seenTargets = new HashSet<IInteractable>();

        public bool IsEnabled => isEnabled && radius > 0f;

        private void Awake()
        {
            EnsureBuffer();
        }

        private void OnValidate()
        {
            if (radius < 0f)
                radius = 0f;

            if (bufferSize < 1)
                bufferSize = 1;

            if (Application.isPlaying)
                EnsureBuffer();
        }

        /// <summary>
        /// 收集球形范围内的可交互目标。
        /// Collider 可以在子物体上，IInteractable 可以挂在父物体上。
        /// </summary>
        public void Collect(in InteractionContext context, List<InteractionCandidate> results)
        {
            if (!IsEnabled || !context.IsValid || results == null)
                return;

            if (_buffer == null)
                EnsureBuffer();

            var actorTransform = context.ActorTransform;
            if (actorTransform == null)
                return;

            _seenTargets.Clear();
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Target != null)
                    _seenTargets.Add(results[i].Target);
            }

            Vector3 origin = actorTransform.position;
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin,
                radius,
                _buffer,
                interactableMask,
                triggerInteraction);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _buffer[i];
                if (hit == null)
                    continue;

                var interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || interactable.InteractionTransform == null)
                    continue;

                if (interactable.InteractionTransform == actorTransform)
                    continue;

                if (!interactable.CanInteract(context))
                    continue;

                if (!_seenTargets.Add(interactable))
                    continue;

                Vector3 targetPosition = interactable.InteractionTransform.position;
                Vector3 toTarget = targetPosition - origin;
                float distance = toTarget.magnitude;
                float facingDot = CalculateFacingDot(actorTransform, toTarget);

                results.Add(new InteractionCandidate(
                    interactable,
                    distance,
                    facingDot,
                    DetectMode.Sphere));
            }
        }

        private void EnsureBuffer()
        {
            int safeSize = bufferSize > 0 ? bufferSize : 1;
            if (_buffer == null || _buffer.Length != safeSize)
                _buffer = new Collider[safeSize];
        }

        private static float CalculateFacingDot(Transform actorTransform, Vector3 toTarget)
        {
            if (actorTransform == null || toTarget.sqrMagnitude <= 0.0001f)
                return 1f;

            return Vector3.Dot(actorTransform.forward, toTarget.normalized);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
