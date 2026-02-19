using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Circle collision radius for hitbox detection.
    /// Touhou convention: player hitbox is very small (0.05-0.1),
    /// enemy hitbox is medium (0.3-0.5), bullet hitbox matches visual (0.1-0.15).
    /// </summary>
    public struct CollisionRadius : IComponentData
    {
        /// <summary>Radius of the circular hitbox in world units.</summary>
        public float Value;
    }
}
