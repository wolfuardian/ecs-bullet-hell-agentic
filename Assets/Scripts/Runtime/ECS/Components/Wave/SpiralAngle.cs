using Unity.Entities;

namespace MyGame.ECS.Wave
{
    /// <summary>
    /// Tracks the current rotation angle for SPIRAL pattern enemies.
    /// Incremented by BulletPatternSystem each time the enemy fires.
    /// </summary>
    public struct SpiralAngle : IComponentData
    {
        /// <summary>Current rotation angle in degrees.</summary>
        public float Value;
    }
}
