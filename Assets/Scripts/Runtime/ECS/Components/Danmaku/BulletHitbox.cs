using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Per-bullet hitbox replacing the old uniform CollisionRadius for enemy bullets.
    /// Shape and dimensions are looked up from BulletShapeTable on spawn.
    /// </summary>
    public struct BulletHitbox : IComponentData
    {
        /// <summary>Hitbox geometry type.</summary>
        public HitboxType Type;

        /// <summary>
        /// Hitbox dimensions. Interpretation depends on Type:
        /// Circle: x = radius (y unused).
        /// Oval: x = half-width, y = half-height.
        /// Rect: x = half-width, y = half-height.
        /// Line: x = half-length, y = half-thickness.
        /// </summary>
        public float2 Size;

        /// <summary>Offset from entity position in local space.</summary>
        public float2 Offset;
    }
}
