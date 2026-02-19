using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Item movement velocity. Separate from Bullet.Velocity to avoid BulletMovementSystem.
    /// </summary>
    public struct ItemVelocity : IComponentData
    {
        /// <summary>Movement velocity in world units per second.</summary>
        public float3 Value;
    }
}
