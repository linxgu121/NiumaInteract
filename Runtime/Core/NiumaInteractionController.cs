using NiumaInteract.Core.Arbitration;
using NiumaInteract.Core.Data;
using NiumaInteract.Core.Detection;
using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Input;
using NiumaInteract.Core.Interface;
using UnityEngine;

namespace NiumaInteract.Core
{
    /// <summary>
    /// NiumaInteract 模块根控制器。
    /// 只负责驱动输入、检测、黑板和仲裁器，不直接实现任何具体交互业务。
    /// </summary>
    public sealed class NiumaInteractionController : MonoBehaviour
    {
        [Header("运行状态")]
        [SerializeField] private bool runOnEnable = true;

        [Header("上下文")]
        [SerializeField] private GameObject actor;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private bool useMainCameraWhenMissing = true;

        [Header("模块引用")]
        [Tooltip("交互检测器组，负责收集当前可交互目标列表。")]
        [SerializeField] private InteractionDetectorGroup detectorGroup;
        [Tooltip("交互输入源，负责提供原始输入快照。")]
        [SerializeField] private InteractionInputSourceBase inputSource;
        [Tooltip("交互门提供者，负责提供交互门实例。")]
        [SerializeField] private MonoBehaviour gateProvider;
        [SerializeField] private bool autoFindReferences = true;

        [Header("黑板")]
        [SerializeField] private float targetLostDelay = 0.2f;

        [Header("仲裁")]
        [SerializeField] private float triggerCooldown = 0.1f;
        [SerializeField] private float priorityWeight = 10f;
        [SerializeField] private float facingWeight = 3f;
        [SerializeField] private float distanceWeight = 2f;
        [SerializeField] private InteractionDistanceFalloffType distanceFalloffType =
            InteractionDistanceFalloffType.Inverse;
        [SerializeField] private float distanceScoreMaxDistance = 5f;
        [SerializeField] private float raycastSourceBonus = 1f;

        private readonly InteractionBlackboard _blackboard = new InteractionBlackboard();
        private InteractionInputPipeline _inputPipeline;
        private InteractionArbiter _arbiter;
        private IInteractionGate _gate;
        private InteractionInputSourceBase _runtimeInputSource;
        private bool _isRunning;
        private bool _hasWarnedInvalidGateProvider;

        /// <summary>
        /// 交互黑板。
        /// UI、调试工具或外部模块应通过黑板事件读取交互状态，而不是直接依赖检测器或仲裁器。
        /// </summary>
        public InteractionBlackboard Blackboard => _blackboard;

        /// <summary>
        /// 当前模块是否正在运行。
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 当前交互上下文快照。
        /// </summary>
        public InteractionContext CurrentContext => BuildContext();

        private void Awake()
        {
            InitializeRuntime();
        }

        private void OnEnable()
        {
            if (runOnEnable)
                SetRunning(true);
        }

        private void OnDisable()
        {
            SetRunning(false);
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            EnsureRuntimeReady();

            float deltaTime = Time.deltaTime;
            var context = BuildContext();
            if (!context.IsValid)
            {
                HandleInvalidContext(deltaTime);
                return;
            }

            var candidates = detectorGroup != null ? detectorGroup.Collect(context) : null;
            _blackboard.SetCandidates(candidates);
            _arbiter.UpdateFocus(context);

            var input = _inputPipeline != null
                ? _inputPipeline.Tick(deltaTime)
                : InteractionInputSnapshot.Empty;

            _blackboard.SetHoldTime(input.HoldTime);

            if (_arbiter.TryProcessInput(input, context, Time.time, out var result))
            {
                _blackboard.PublishInteractionResult(result);

                if (result.Succeeded)
                    ClearHoldTimeAfterSuccess();
            }

            _blackboard.Tick(deltaTime);
        }

        /// <summary>
        /// 设置模块运行状态。
        /// 停止时会清空输入、候选和焦点，避免旧状态影响下次启用。
        /// </summary>
        public void SetRunning(bool running)
        {
            if (_isRunning == running)
                return;

            _isRunning = running;

            if (_isRunning)
            {
                EnsureRuntimeReady();
                return;
            }

            ClearRuntimeState();
        }

        /// <summary>
        /// 设置输入阻塞状态。
        /// UI 菜单、对话、剧情镜头可以调用它临时禁止交互输入。
        /// </summary>
        public void SetInputBlocked(bool blocked, bool clearBufferedInput = true)
        {
            EnsureRuntimeReady();
            _inputPipeline?.SetBlocked(blocked, clearBufferedInput);

            if (blocked && clearBufferedInput)
                _blackboard.SetHoldTime(0f);
        }

        /// <summary>
        /// 外部系统写入吸附锁定目标。
        /// 例如 StickyAim、剧情强制交互或教程引导可以使用该入口。
        /// </summary>
        public void SetLockedTarget(IInteractable target)
        {
            _blackboard.SetLockedTarget(target);
        }

        /// <summary>
        /// 重新收集检测器引用。
        /// 当运行时动态添加或替换检测器组件后调用。
        /// </summary>
        public void RebuildDetectors()
        {
            detectorGroup?.RebuildDetectors();
        }

        /// <summary>
        /// 立即清空运行时状态。
        /// 不销毁任何组件，只清理本模块缓存的输入、候选、焦点和结果。
        /// </summary>
        public void ClearRuntimeState()
        {
            _inputPipeline?.CancelCurrentHold(true);
            _blackboard.Clear();
        }

        /// <summary>
        /// 手动设置 Actor。
        /// 玩家对象重建或切换控制角色时调用。
        /// </summary>
        public void SetActor(GameObject newActor)
        {
            actor = newActor;
            ClearRuntimeState();
        }

        /// <summary>
        /// 手动设置视角相机。
        /// 相机切换或分屏时调用。
        /// </summary>
        public void SetViewCamera(Camera newViewCamera)
        {
            viewCamera = newViewCamera;
        }

        private void InitializeRuntime()
        {
            FindReferencesIfNeeded();
            ResolveGate();

            _blackboard.TargetLostDelay = targetLostDelay;
            _inputPipeline = new InteractionInputPipeline(inputSource);
            _runtimeInputSource = inputSource;
            _arbiter = new InteractionArbiter(_blackboard, _gate);
            ApplyArbiterSettings();
        }

        private void EnsureRuntimeReady()
        {
            if (_arbiter == null || _inputPipeline == null)
            {
                InitializeRuntime();
                return;
            }

            FindReferencesIfNeeded();
            RefreshInputPipelineIfNeeded();
            RefreshArbiterGateIfNeeded();
            ResolveCameraIfNeeded();
            ApplyRuntimeSettings();
        }

        private void FindReferencesIfNeeded()
        {
            if (actor == null)
                actor = gameObject;

            ResolveCameraIfNeeded();

            if (!autoFindReferences)
                return;

            if (detectorGroup == null)
                detectorGroup = GetComponentInChildren<InteractionDetectorGroup>(true);

            if (inputSource == null)
                inputSource = GetComponentInChildren<InteractionInputSourceBase>(true);
        }

        private void ResolveCameraIfNeeded()
        {
            if (viewCamera == null && useMainCameraWhenMissing)
                viewCamera = Camera.main;
        }

        private void ResolveGate()
        {
            _gate = gateProvider as IInteractionGate;

            if (gateProvider == null || _gate != null)
            {
                _hasWarnedInvalidGateProvider = false;
                return;
            }

            if (!_hasWarnedInvalidGateProvider)
            {
                Debug.LogWarning(
                    $"{nameof(NiumaInteractionController)} 的 Gate Provider 没有实现 {nameof(IInteractionGate)}。",
                    this);
                _hasWarnedInvalidGateProvider = true;
            }
        }

        private void RefreshInputPipelineIfNeeded()
        {
            if (_runtimeInputSource == inputSource)
                return;

            _inputPipeline = new InteractionInputPipeline(inputSource);
            _runtimeInputSource = inputSource;
        }

        private void RefreshArbiterGateIfNeeded()
        {
            var previousGate = _gate;
            ResolveGate();

            if (ReferenceEquals(previousGate, _gate))
                return;

            _arbiter.SetGate(_gate);
        }

        private InteractionContext BuildContext()
        {
            ResolveCameraIfNeeded();
            return new InteractionContext(actor, viewCamera);
        }

        private void ApplyRuntimeSettings()
        {
            _blackboard.TargetLostDelay = targetLostDelay;
            ApplyArbiterSettings();
        }

        private void ApplyArbiterSettings()
        {
            _arbiter.TriggerCooldown = triggerCooldown;
            _arbiter.PriorityWeight = priorityWeight;
            _arbiter.FacingWeight = facingWeight;
            _arbiter.DistanceWeight = distanceWeight;
            _arbiter.DistanceFalloffType = distanceFalloffType;
            _arbiter.DistanceScoreMaxDistance = distanceScoreMaxDistance;
            _arbiter.RaycastSourceBonus = raycastSourceBonus;
        }

        private void HandleInvalidContext(float deltaTime)
        {
            _inputPipeline?.CancelCurrentHold(true);
            _blackboard.SetHoldTime(0f);
            _blackboard.SetCandidates(null);
            _blackboard.SetCurrentTarget(null, true);
            _blackboard.Tick(deltaTime);
        }

        private void ClearHoldTimeAfterSuccess()
        {
            _inputPipeline?.ResetHoldTime();
            _blackboard.SetHoldTime(0f);
        }

        private void OnValidate()
        {
            targetLostDelay = targetLostDelay > 0f ? targetLostDelay : 0f;
            triggerCooldown = triggerCooldown > 0f ? triggerCooldown : 0f;
            priorityWeight = priorityWeight > 0f ? priorityWeight : 0f;
            facingWeight = facingWeight > 0f ? facingWeight : 0f;
            distanceWeight = distanceWeight > 0f ? distanceWeight : 0f;
            distanceScoreMaxDistance = distanceScoreMaxDistance > 0.0001f ? distanceScoreMaxDistance : 0.0001f;
            raycastSourceBonus = raycastSourceBonus > 0f ? raycastSourceBonus : 0f;
        }
    }
}
