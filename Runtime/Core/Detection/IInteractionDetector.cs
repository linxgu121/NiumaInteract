using System.Collections.Generic;
using NiumaInteract.Core.Data;

namespace NiumaInteract.Core.Detection
{
    /// <summary>
    /// 交互检测器接口。
    /// 检测器只负责输出候选目标，不负责排序、焦点裁决或触发交互。
    /// </summary>
    public interface IInteractionDetector
    {
        /// <summary>
        /// 检测器是否启用。
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 收集当前帧检测到的候选目标。
        /// </summary>
        void Collect(in InteractionContext context, List<InteractionCandidate> results);
    }
}
