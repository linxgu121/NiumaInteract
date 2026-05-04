namespace NiumaInteract.Core.Enum
{
    /// <summary>
    /// 交互距离评分的衰减曲线类型。
    /// 后续可由配置或 Controller 暴露到 Inspector，方便按玩法场景调参。
    /// </summary>
    public enum InteractionDistanceFalloffType
    {
        /// <summary>
        /// 反比衰减：距离越近分越高，远距离仍保留少量分数。
        /// </summary>
        Inverse = 0,

        /// <summary>
        /// 线性衰减：超过最大评分距离后归零。
        /// </summary>
        Linear = 1,

        /// <summary>
        /// 范围内固定给满分，范围外归零。
        /// 适合只关心“是否在有效范围内”的交互。
        /// </summary>
        Binary = 2
    }
}
