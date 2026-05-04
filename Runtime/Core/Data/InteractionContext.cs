using UnityEngine;

namespace NiumaInteract.Core.Data
{
    /// <summary>
    /// 交互上下文。
    /// 描述“谁正在尝试交互”，不携带具体业务系统引用，也不保存候选列表。
    /// </summary>
    public readonly struct InteractionContext
    {
        /// <summary>
        /// 发起交互的主体，通常是玩家对象。
        /// </summary>
        public readonly GameObject Actor;

        /// <summary>
        /// 交互检测使用的视角相机。
        /// Raycast 检测和 StickyAim 都依赖该相机。
        /// </summary>
        public readonly Camera ViewCamera;

        /// <summary>
        /// 主体 Transform 统一从 Actor 派生，避免 Actor 与 ActorTransform 两份引用不一致。
        /// </summary>
        public Transform ActorTransform => Actor != null ? Actor.transform : null;

        /// <summary>
        /// 上下文是否可用于完整交互检测。
        /// 这里要求 Actor 与 ViewCamera 都存在，避免射线检测或吸附检测在运行时缺引用。
        /// </summary>
        public bool IsValid => Actor != null && ViewCamera != null;

        public InteractionContext(GameObject actor, Camera viewCamera)
        {
            Actor = actor;
            ViewCamera = viewCamera;
        }
    }
}
