using NiumaInteract.Core.Data;
using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Input;
using NiumaInteract.Core.Interface;

namespace NiumaInteract.Core.Arbitration
{
    /// <summary>
    /// 交互仲裁器。
    /// 每帧负责焦点选择；短按在松手时触发，长按达到阈值后立刻触发。
    /// </summary>
    public sealed class InteractionArbiter
    {
        private readonly InteractionBlackboard _blackboard;
        private IInteractionGate _gate;
        private float _nextAllowedTriggerTime;
        private IInteractable _holdTarget;
        private float _holdBaselineTime;
        private bool _longPressConsumed;
        private IInteractable _longPressFailedTarget;

        /// <summary>
        /// 触发冷却，防止松手、输入缓冲或同帧多次调用造成重复触发。
        /// </summary>
        public float TriggerCooldown { get; set; } = 0.1f;

        /// <summary>
        /// 目标自身优先级权重。
        /// </summary>
        public float PriorityWeight { get; set; } = 10f;

        /// <summary>
        /// 玩家朝向权重。FacingDot 会先截断到 [0, 1] 再参与评分。
        /// </summary>
        public float FacingWeight { get; set; } = 3f;

        /// <summary>
        /// 距离权重。距离越近，距离分越高。
        /// </summary>
        public float DistanceWeight { get; set; } = 2f;

        /// <summary>
        /// 距离评分衰减类型。
        /// 后续由 Controller 或配置 SO 暴露到 Inspector 时，可以作为下拉项选择。
        /// </summary>
        public InteractionDistanceFalloffType DistanceFalloffType { get; set; } =
            InteractionDistanceFalloffType.Inverse;

        /// <summary>
        /// 距离评分的最大参考距离。
        /// Linear 和 Binary 曲线会使用该值，Inverse 曲线不依赖该值。
        /// </summary>
        public float DistanceScoreMaxDistance { get; set; } = 5f;

        /// <summary>
        /// Raycast 检测来源加分。远距离瞄准命中时，目标会获得额外评分。
        /// </summary>
        public float RaycastSourceBonus { get; set; } = 1f;

        public InteractionArbiter(InteractionBlackboard blackboard, IInteractionGate gate = null)
        {
            _blackboard = blackboard;
            _gate = gate;
        }

        /// <summary>
        /// 替换交互门禁引用。
        /// 该方法不会清空冷却和按住状态，避免运行时切换 Gate 时打断玩家当前输入。
        /// </summary>
        public void SetGate(IInteractionGate gate)
        {
            _gate = gate;
        }

        /// <summary>
        /// 每帧更新焦点目标。
        /// LockedTarget 绝对优先；没有锁定目标时，从候选列表中按评分选择焦点。
        /// </summary>
        public void UpdateFocus(in InteractionContext context)
        {
            if (_blackboard == null)
                return;

            if (!context.IsValid)
            {
                _blackboard.SetCurrentTarget(null);
                return;
            }

            var lockedTarget = _blackboard.LockedTarget;
            if (IsTargetUsable(lockedTarget, context))
            {
                _blackboard.SetCurrentTarget(lockedTarget);
                return;
            }

            _blackboard.SetCurrentTarget(SelectBestTarget(context));
        }

        /// <summary>
        /// 尝试处理一次输入触发。
        /// 短按在松手时触发；长按达到目标阈值后立刻触发，不等待松手。
        /// 没有产生交互结果时返回 false，调用方不应该发布结果事件。
        /// </summary>
        public bool TryProcessInput(
            in InteractionInputSnapshot input,
            in InteractionContext context,
            float currentTime,
            out InteractionResult result)
        {
            result = default;

            if (!input.IsHolding && !input.ReleasedThisFrame)
            {
                ResetHoldState();
                return false;
            }

            var target = _blackboard?.CurrentTarget;
            SyncHoldState(input, target);

            if (_longPressConsumed)
            {
                if (input.ReleasedThisFrame)
                    ResetHoldState();

                return false;
            }

            float targetHoldTime = GetTargetHoldTime(input);
            bool shouldTriggerLongPress = ShouldTriggerLongPress(input, target, targetHoldTime);

            if (!shouldTriggerLongPress && !input.ReleasedThisFrame)
                return false;

            if (shouldTriggerLongPress && ReferenceEquals(_longPressFailedTarget, target))
            {
                if (input.ReleasedThisFrame)
                    ResetHoldState();

                return false;
            }

            if (!context.IsValid)
                return FinishWithResult(
                    InteractionResult.Fail(InteractionFailReason.InvalidContext),
                    input.ReleasedThisFrame,
                    shouldTriggerLongPress,
                    target,
                    out result);

            if (_gate != null && !_gate.CanInteract)
                return FinishWithResult(
                    InteractionResult.Fail(InteractionFailReason.GateBlocked),
                    input.ReleasedThisFrame,
                    shouldTriggerLongPress,
                    target,
                    out result);

            if (target == null)
                return FinishWithResult(
                    InteractionResult.Fail(InteractionFailReason.NoTarget),
                    input.ReleasedThisFrame,
                    shouldTriggerLongPress,
                    target,
                    out result);

            if (!target.CanInteract(context))
                return FinishWithResult(
                    InteractionResult.Fail(InteractionFailReason.TargetRejected, target),
                    input.ReleasedThisFrame,
                    shouldTriggerLongPress,
                    target,
                    out result);

            if (currentTime < _nextAllowedTriggerTime)
                return FinishWithResult(
                    InteractionResult.Fail(InteractionFailReason.Cooldown, target),
                    input.ReleasedThisFrame,
                    false,
                    null,
                    out result);

            var kind = shouldTriggerLongPress ? InteractKind.Long : ResolveReleaseInteractKind(target, targetHoldTime);
            if (!SupportsKind(target, kind))
                return FinishWithResult(
                    InteractionResult.Fail(InteractionFailReason.UnsupportedKind, target, kind),
                    input.ReleasedThisFrame,
                    shouldTriggerLongPress,
                    target,
                    out result);

            var request = new InteractionRequest(kind, context, targetHoldTime);
            target.Interact(request);

            if (kind == InteractKind.Long)
                _longPressConsumed = true;

            _nextAllowedTriggerTime = currentTime + (TriggerCooldown > 0f ? TriggerCooldown : 0f);
            return FinishWithResult(
                InteractionResult.Succeed(target, kind),
                input.ReleasedThisFrame,
                false,
                null,
                out result);
        }

        private IInteractable SelectBestTarget(in InteractionContext context)
        {
            if (_blackboard == null)
                return null;

            var candidates = _blackboard.Candidates;
            IInteractable bestTarget = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!candidate.IsValid)
                    continue;

                var target = candidate.Target;
                if (!IsTargetUsable(target, context))
                    continue;

                float score = CalculateScore(candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        private bool IsTargetUsable(IInteractable target, in InteractionContext context)
        {
            return context.IsValid &&
                   target != null &&
                   target.InteractionTransform != null &&
                   target.CanInteract(context);
        }

        private float CalculateScore(in InteractionCandidate candidate)
        {
            float priorityScore = candidate.Target.Priority * PriorityWeight;
            float facingScore = InteractionScoring.EvaluateFacing(candidate.FacingDot) * FacingWeight;
            float distanceScore = InteractionScoring.EvaluateDistance(
                candidate.Distance,
                DistanceFalloffType,
                DistanceScoreMaxDistance) * DistanceWeight;
            float sourceScore = (candidate.SourceMode & DetectMode.Raycast) != 0 ? RaycastSourceBonus : 0f;

            return priorityScore + facingScore + distanceScore + sourceScore;
        }

        private void SyncHoldState(in InteractionInputSnapshot input, IInteractable target)
        {
            if (!input.IsHolding)
                return;

            if (input.PressedThisFrame)
            {
                _holdTarget = target;
                _holdBaselineTime = 0f;
                _longPressConsumed = false;
                _longPressFailedTarget = null;
                return;
            }

            if (!ReferenceEquals(_holdTarget, target))
            {
                _holdTarget = target;
                _holdBaselineTime = input.HoldTime;
                _longPressFailedTarget = null;
            }
        }

        private float GetTargetHoldTime(in InteractionInputSnapshot input)
        {
            return InteractionScoring.Max(0f, input.HoldTime - _holdBaselineTime);
        }

        private bool ShouldTriggerLongPress(
            in InteractionInputSnapshot input,
            IInteractable target,
            float targetHoldTime)
        {
            return input.IsHolding &&
                   target != null &&
                   target.LongPressDuration > 0f &&
                   targetHoldTime >= target.LongPressDuration &&
                   SupportsKind(target, InteractKind.Long);
        }

        private void ResetHoldState()
        {
            _holdTarget = null;
            _holdBaselineTime = 0f;
            _longPressConsumed = false;
            _longPressFailedTarget = null;
        }

        private bool FinishWithResult(
            InteractionResult value,
            bool resetHoldState,
            bool markLongPressFailure,
            IInteractable longPressTarget,
            out InteractionResult result)
        {
            result = value;

            if (markLongPressFailure && !value.Succeeded)
                _longPressFailedTarget = longPressTarget;

            if (resetHoldState)
                ResetHoldState();

            return true;
        }

        private static InteractKind ResolveReleaseInteractKind(IInteractable target, float holdTime)
        {
            if (target.LongPressDuration > 0f && holdTime >= target.LongPressDuration)
                return InteractKind.Long;

            return InteractKind.Short;
        }

        private static bool SupportsKind(IInteractable target, InteractKind kind)
        {
            return (target.SupportedKinds & kind) == kind;
        }
    }
}
