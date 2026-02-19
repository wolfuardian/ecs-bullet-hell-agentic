using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Duration of invincibility granted after taking damage (seconds).
    /// Baked from PlayerAuthoring. Read by collision systems to reset InvincibilityTimer.
    /// </summary>
    public struct InvincibilityDuration : IComponentData
    {
        /// <summary>How many seconds of invincibility to grant after each hit.</summary>
        public float Value;
    }
}
