using Unity.Entities;

namespace MyGame.ECS.Graze
{
    /// <summary>
    /// Tracks graze count and detection radius on the player entity.
    /// Graze occurs when an enemy bullet passes close to the player
    /// without actually hitting the collision hitbox.
    /// </summary>
    public struct GrazeData : IComponentData
    {
        /// <summary>Total accumulated graze count.</summary>
        public int Count;

        /// <summary>Detection radius for graze (larger than CollisionRadius).</summary>
        public float GrazeRadius;
    }
}
