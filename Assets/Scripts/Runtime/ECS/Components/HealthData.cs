using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Hit points for damageable entities (player and enemies).
    /// When Current reaches 0 or below, the entity is eligible for death processing.
    /// </summary>
    public struct HealthData : IComponentData
    {
        /// <summary>Current hit points. Entity dies when this reaches 0.</summary>
        public int Current;

        /// <summary>Maximum hit points (for future HP bar display).</summary>
        public int Max;
    }
}
