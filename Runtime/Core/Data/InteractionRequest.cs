using NiumaInteract.Core.Enum;

namespace NiumaInteract.Core.Data
{
    /// <summary>
    /// 一次已经被仲裁器确认的交互请求。
    /// 请求传给目标对象执行，因此不重复保存 Target，目标对象自身就是执行者。
    /// </summary>
    public readonly struct InteractionRequest
    {
        public readonly InteractKind Kind;
        public readonly InteractionContext Context;
        public readonly float HoldTime;

        public InteractionRequest(
            InteractKind kind,
            in InteractionContext context,
            float holdTime)
        {
            Kind = kind;
            Context = context;
            HoldTime = holdTime;
        }
    }
}
