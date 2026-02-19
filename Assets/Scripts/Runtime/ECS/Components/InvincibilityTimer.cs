using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Remaining invincibility time in seconds.
    /// While Value > 0, the entity cannot take damage from collisions.
    /// Decremented each frame by InvincibilitySystem.
    /// </summary>
    public struct InvincibilityTimer : IComponentData
    {
        /// <summary>Remaining invincibility time in seconds. Immune while > 0.</summary>
        public float Value;
    }
}
