namespace NiumaInteract.Core.Input
{
    /// <summary>
    /// 交互输入管线。
    /// 负责把原始输入转换成稳定输入快照，只计时，不裁决目标和交互类型。
    /// </summary>
    public sealed class InteractionInputPipeline
    {
        private readonly InteractionInputSourceBase _inputSource;
        private InteractionRawInput _rawInput;
        private InteractionInputSnapshot _snapshot;
        private bool _wasHolding;
        private float _holdTime;
        private bool _isBlocked;
        private bool _waitReleaseBeforeNextPress;

        public InteractionInputSnapshot Snapshot => _snapshot;
        public bool IsBlocked => _isBlocked || (_inputSource != null && _inputSource.IsBlocked);

        public InteractionInputPipeline(InteractionInputSourceBase inputSource)
        {
            _inputSource = inputSource;
            _snapshot = InteractionInputSnapshot.Empty;
        }

        /// <summary>
        /// 设置管线阻塞状态。
        /// 被阻塞时会清空按住时长，避免解除阻塞后残留一次错误的松手触发。
        /// </summary>
        public void SetBlocked(bool blocked, bool clearBufferedInput = true)
        {
            _isBlocked = blocked;

            if (blocked && clearBufferedInput)
                CancelCurrentHold(true);
        }

        /// <summary>
        /// 清空全部输入状态。
        /// 该方法用于重新初始化输入管线，不会额外要求玩家松手后才能再次触发。
        /// </summary>
        public void Clear()
        {
            _rawInput.Clear();
            _snapshot = InteractionInputSnapshot.Empty;
            _wasHolding = false;
            _holdTime = 0f;
            _waitReleaseBeforeNextPress = false;
        }

        /// <summary>
        /// 只清零当前按住时长，保留按住状态。
        /// 适用于仲裁器已触发一次交互，但仍希望知道玩家当前是否还按着交互键的场景。
        /// </summary>
        public void ResetHoldTime()
        {
            _holdTime = 0f;

            if (_snapshot.IsHolding)
            {
                _snapshot = new InteractionInputSnapshot(
                    _snapshot.PressedThisFrame,
                    _snapshot.ReleasedThisFrame,
                    true,
                    0f);
            }
        }

        /// <summary>
        /// 取消当前按住状态。
        /// requireReleaseBeforeNextPress 为 true 时，会忽略当前仍然按住的输入，直到玩家松手后才允许产生下一次 PressedThisFrame。
        /// </summary>
        public void CancelCurrentHold(bool requireReleaseBeforeNextPress = true)
        {
            _rawInput.Clear();
            _snapshot = InteractionInputSnapshot.Empty;
            _wasHolding = false;
            _holdTime = 0f;
            _waitReleaseBeforeNextPress = requireReleaseBeforeNextPress;
        }

        /// <summary>
        /// 每帧调用一次，输出本帧输入快照。
        /// deltaTime 由外部传入，便于测试和统一时间驱动。
        /// </summary>
        public InteractionInputSnapshot Tick(float deltaTime)
        {
            if (IsBlocked || _inputSource == null)
            {
                CancelCurrentHold(true);
                return _snapshot;
            }

            _rawInput.Clear();
            _inputSource.FetchRawInput(ref _rawInput);

            bool rawHolding = _rawInput.InteractHeld;
            if (_waitReleaseBeforeNextPress)
            {
                if (rawHolding)
                {
                    _snapshot = InteractionInputSnapshot.Empty;
                    return _snapshot;
                }

                _waitReleaseBeforeNextPress = false;
            }

            bool isHolding = rawHolding;
            bool pressedThisFrame = isHolding && !_wasHolding;
            bool releasedThisFrame = !isHolding && _wasHolding;

            if (isHolding)
                _holdTime += deltaTime > 0f ? deltaTime : 0f;

            float snapshotHoldTime = releasedThisFrame ? _holdTime : (isHolding ? _holdTime : 0f);
            _snapshot = new InteractionInputSnapshot(
                pressedThisFrame,
                releasedThisFrame,
                isHolding,
                snapshotHoldTime);

            if (!isHolding)
                _holdTime = 0f;

            _wasHolding = isHolding;
            return _snapshot;
        }
    }
}
