using UnityEngine;

namespace NiumaInteract.Core.Input
{
    /// <summary>
    /// 交互输入源基类。
    /// 具体项目可以用 Input System、旧输入系统或 AI 输入继承它，只要填充原始输入即可。
    /// </summary>
    public abstract class InteractionInputSourceBase : MonoBehaviour
    {
        [SerializeField] private bool isBlocked;

        /// <summary>
        /// 输入源是否被阻塞。被阻塞时输入管线会输出空快照并清理按住状态。
        /// </summary>
        public bool IsBlocked => isBlocked;

        /// <summary>
        /// 设置输入源阻塞状态。
        /// clearBufferedInput 由输入管线处理，这里只记录输入源是否允许读取。
        /// </summary>
        public void SetBlocked(bool blocked)
        {
            isBlocked = blocked;
        }

        /// <summary>
        /// 读取当前帧原始输入。
        /// 该方法只写入按键事实，不计算按住时长。
        /// </summary>
        public abstract void FetchRawInput(ref InteractionRawInput rawInput);
    }
}
