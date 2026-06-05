using System.Collections.Generic;
using NiumaInteract.Core.Data;
using UnityEngine;

namespace NiumaInteract.Core.Detection
{
    /// <summary>
    /// 交互检测器组。
    /// 统一驱动多个检测器，并合并它们输出的候选目标。
    /// </summary>
    public sealed class InteractionDetectorGroup : MonoBehaviour
    {
        [Tooltip("手动指定的检测器脚本列表。近距离交互拖 SphereInteractionDetector；远距离瞄准拖 RaycastInteractionDetector；需要两种检测时两个都拖。")]
        [SerializeField] private MonoBehaviour[] detectorProviders;
        [Tooltip("如果没有手动指定检测器，则自动查找所有子物体上的 IInteractionDetector 组件并添加到检测器列表中。")]
        [SerializeField] private bool autoFindChildDetectors = true;

        private readonly List<IInteractionDetector> _detectors = new List<IInteractionDetector>();
        private readonly List<InteractionCandidate> _candidates = new List<InteractionCandidate>();

        public IReadOnlyList<InteractionCandidate> Candidates => _candidates;

        private void Awake()
        {
            RebuildDetectors();
        }

        /// <summary>
        /// 重新收集检测器引用。
        /// 允许检测器作为独立 MonoBehaviour 挂在 Player 或子物体上，再由组统一驱动。
        /// </summary>
        public void RebuildDetectors()
        {
            _detectors.Clear();

            if (detectorProviders == null)
            {
                AutoFindDetectorsIfNeeded();
                return;
            }

            for (int i = 0; i < detectorProviders.Length; i++)
            {
                if (detectorProviders[i] is IInteractionDetector detector)
                    _detectors.Add(detector);
            }

            AutoFindDetectorsIfNeeded();
        }

        /// <summary>
        /// 收集所有检测器输出的候选目标。
        /// 该方法不排序、不裁决，只做检测结果合并。
        /// </summary>
        /// <returns>
        /// 返回内部缓存列表的只读视图。
        /// 调用方应在返回后立即消费数据，不要缓存引用到下一帧或协程中延迟读取。
        /// </returns>
        public IReadOnlyList<InteractionCandidate> Collect(in InteractionContext context)
        {
            _candidates.Clear();

            for (int i = 0; i < _detectors.Count; i++)
            {
                var detector = _detectors[i];
                if (detector == null || !detector.IsEnabled)
                    continue;

                detector.Collect(context, _candidates);
            }

            return _candidates;
        }

        /// <summary>
        /// 如果启用了自动查找选项但没有手动指定检测器
        /// 则在子物体中查找 SphereInteractionDetector、RaycastInteractionDetector 等检测器脚本并添加到检测器列表中。
        /// </summary>
        private void AutoFindDetectorsIfNeeded()
        {
            if (!autoFindChildDetectors || _detectors.Count > 0)
                return;

            var behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == this)
                    continue;

                if (behaviours[i] is IInteractionDetector detector)
                    _detectors.Add(detector);
            }
        }
    }
}
