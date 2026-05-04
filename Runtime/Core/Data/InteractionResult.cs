using NiumaInteract.Core.Enum;
using NiumaInteract.Core.Interface;

namespace NiumaInteract.Core.Data
{
    /// <summary>
    /// 交互仲裁与触发结果。
    /// 该结果由仲裁器生成，用于说明本次交互请求是否被触发、触发了哪种交互，以及失败原因。
    /// </summary>
    public readonly struct InteractionResult
    {
        public readonly bool Succeeded;
        public readonly IInteractable Target;
        public readonly InteractKind Kind;
        public readonly InteractionFailReason FailReason;

        public InteractionResult(
            bool succeeded,
            IInteractable target,
            InteractKind kind,
            InteractionFailReason failReason)
        {
            Succeeded = succeeded;
            Target = target;
            Kind = kind;
            FailReason = failReason;
        }

        public static InteractionResult Succeed(IInteractable target, InteractKind kind)
        {
            return new InteractionResult(true, target, kind, InteractionFailReason.None);
        }

        public static InteractionResult Fail(
            InteractionFailReason reason,
            IInteractable target = null,
            InteractKind kind = InteractKind.None)
        {
            return new InteractionResult(false, target, kind, reason);
        }
    }
}
