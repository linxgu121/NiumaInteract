using System;
using System.Collections.Generic;
using NiumaInteract.Core.Data;
using NiumaInteract.Core.Interface;

namespace NiumaInteract.Core
{
    /// <summary>
    /// 交互数据黑板。
    /// 黑板只保存运行时快照和派发事件，不负责检测、排序、裁决或执行业务交互。
    /// </summary>
    public sealed class InteractionBlackboard
    {
        private const float CandidateFloatTolerance = 0.0001f;
        private readonly List<InteractionCandidate> _candidates = new List<InteractionCandidate>();
        private IInteractable _pendingLostTarget;
        private float _targetLostTimer;
        private bool _isWaitingTargetLost;

        /// <summary>
        /// 当前候选目标列表，由检测层写入，由仲裁器读取。
        /// </summary>
        public IReadOnlyList<InteractionCandidate> Candidates => _candidates;

        /// <summary>
        /// 当前焦点目标，由仲裁器写入，由 UI 桥接、控制器或调试工具读取。
        /// </summary>
        public IInteractable CurrentTarget { get; private set; }

        /// <summary>
        /// 当前吸附锁定目标。锁定目标通常由 StickyAim 写入，仲裁器排序时优先使用。
        /// </summary>
        public IInteractable LockedTarget { get; private set; }

        /// <summary>
        /// 当前按住时长。该值来自输入层，不包含长按进度。
        /// </summary>
        public float HoldTime { get; private set; }

        /// <summary>
        /// 最近一次交互仲裁或触发结果。
        /// </summary>
        public InteractionResult LastResult { get; private set; }

        /// <summary>
        /// 是否已经发布过交互结果。
        /// 用于区分 LastResult 的默认值和一次真实的失败结果。
        /// </summary>
        public bool HasLastResult { get; private set; }

        /// <summary>
        /// 目标丢失延迟，避免准星或范围边界抖动导致 UI 提示闪烁。
        /// </summary>
        public float TargetLostDelay { get; set; } = 0.2f;

        public event Action<IInteractable> OnTargetChanged;
        public event Action<IReadOnlyList<InteractionCandidate>> OnCandidatesChanged;
        public event Action<IInteractable> OnLockedTargetChanged;
        public event Action<float> OnHoldTimeChanged;
        public event Action<InteractionResult> OnInteractionResult;

        /// <summary>
        /// 每帧驱动黑板内部延迟逻辑。
        /// 当前只处理目标丢失延迟，不做检测或排序。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isWaitingTargetLost)
                return;

            _targetLostTimer += deltaTime > 0f ? deltaTime : 0f;
            if (_targetLostTimer < TargetLostDelay)
                return;

            if (CurrentTarget == _pendingLostTarget)
                ApplyCurrentTarget(null);

            ClearPendingTargetLost();
        }

        /// <summary>
        /// 写入候选列表。
        /// 检测层可以每帧调用，黑板会复用内部列表，避免把外部列表长期持有。
        /// </summary>
        public void SetCandidates(IReadOnlyList<InteractionCandidate> candidates)
        {
            if (AreCandidatesSame(candidates))
                return;

            _candidates.Clear();

            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].IsValid)
                        _candidates.Add(candidates[i]);
                }
            }

            OnCandidatesChanged?.Invoke(_candidates);
        }

        /// <summary>
        /// 写入当前焦点目标。
        /// target 为 null 时默认走目标丢失延迟；immediate 为 true 时立刻清空。
        /// </summary>
        public void SetCurrentTarget(IInteractable target, bool immediate = false)
        {
            if (target != null)
            {
                ClearPendingTargetLost();

                if (target != CurrentTarget)
                    ApplyCurrentTarget(target);

                return;
            }

            if (CurrentTarget == null)
                return;

            if (immediate || TargetLostDelay <= 0f)
            {
                ApplyCurrentTarget(null);
                ClearPendingTargetLost();
                return;
            }

            if (!_isWaitingTargetLost)
            {
                _pendingLostTarget = CurrentTarget;
                _targetLostTimer = 0f;
                _isWaitingTargetLost = true;
            }
        }

        /// <summary>
        /// 写入吸附锁定目标。
        /// </summary>
        public void SetLockedTarget(IInteractable target)
        {
            if (LockedTarget == target)
                return;

            LockedTarget = target;
            OnLockedTargetChanged?.Invoke(LockedTarget);
        }

        /// <summary>
        /// 写入当前按住时长。
        /// 输入层只提供 HoldTime，长按进度由 UI 桥接层结合 CurrentTarget.LongPressDuration 计算。
        /// </summary>
        public void SetHoldTime(float holdTime)
        {
            float safeHoldTime = holdTime > 0f ? holdTime : 0f;
            if (HoldTime == safeHoldTime)
                return;

            HoldTime = safeHoldTime;
            OnHoldTimeChanged?.Invoke(HoldTime);
        }

        /// <summary>
        /// 发布一次交互结果。
        /// 该结果由仲裁器生成，黑板只保存并派发事件。
        /// </summary>
        public void PublishInteractionResult(in InteractionResult result)
        {
            LastResult = result;
            HasLastResult = true;
            OnInteractionResult?.Invoke(LastResult);
        }

        /// <summary>
        /// 清空所有运行时快照。
        /// 通常在模块停用、切场景或玩家对象重建时调用。
        /// </summary>
        public void Clear()
        {
            _candidates.Clear();
            ClearPendingTargetLost();

            ApplyCurrentTarget(null);
            SetLockedTarget(null);
            SetHoldTime(0f);
            LastResult = default;
            HasLastResult = false;

            OnCandidatesChanged?.Invoke(_candidates);
        }

        private void ApplyCurrentTarget(IInteractable target)
        {
            if (CurrentTarget == target)
                return;

            CurrentTarget = target;
            OnTargetChanged?.Invoke(CurrentTarget);
        }

        private void ClearPendingTargetLost()
        {
            _pendingLostTarget = null;
            _targetLostTimer = 0f;
            _isWaitingTargetLost = false;
        }

        private bool AreCandidatesSame(IReadOnlyList<InteractionCandidate> candidates)
        {
            int incomingCount = candidates?.Count ?? 0;
            int validCount = 0;

            for (int i = 0; i < incomingCount; i++)
            {
                if (candidates[i].IsValid)
                    validCount++;
            }

            if (_candidates.Count != validCount)
                return false;

            int index = 0;
            for (int i = 0; i < incomingCount; i++)
            {
                var candidate = candidates[i];
                if (!candidate.IsValid)
                    continue;

                var current = _candidates[index];
                if (current.Target != candidate.Target ||
                    !Approximately(current.Distance, candidate.Distance) ||
                    !Approximately(current.FacingDot, candidate.FacingDot) ||
                    current.SourceMode != candidate.SourceMode)
                {
                    return false;
                }

                index++;
            }

            return true;
        }

        private static bool Approximately(float a, float b)
        {
            return Math.Abs(a - b) <= CandidateFloatTolerance;
        }
    }
}
