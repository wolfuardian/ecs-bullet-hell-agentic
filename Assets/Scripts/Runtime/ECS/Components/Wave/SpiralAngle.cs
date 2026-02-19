using Unity.Entities;

namespace MyGame.ECS.Wave
{
    /// <summary>
    /// [DEPRECATED] Use MyGame.ECS.Danmaku.DanmakuSpiralAngle instead.
    /// Kept for backward compatibility with legacy BulletPatternSystem.
    /// </summary>
    public struct SpiralAngle : IComponentData
    {
        /// <summary>Current rotation angle in degrees.</summary>
        public float Value;
    }
}
