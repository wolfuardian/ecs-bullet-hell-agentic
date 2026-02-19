using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Zero-size tag marking a bullet as fired by the player.
    /// Added by BulletSpawnSystem on instantiation.
    /// Used by collision systems to distinguish player bullets from enemy bullets.
    /// </summary>
    public struct PlayerBulletTag : IComponentData
    {
    }
}
