using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Tracks the current rotation angle for Spiral pattern enemies.
    /// Incremented by DanmakuPatternSystem each time the enemy fires.
    /// </summary>
    public struct DanmakuSpiralAngle : IComponentData
    {
        /// <summary>Current rotation angle in radians.</summary>
        public float Value;
    }
}
