using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Zero-size tag marking a bullet as fired by an enemy.
    /// Added by EnemyBulletSpawnSystem on instantiation.
    /// Used by collision systems to distinguish enemy bullets from player bullets.
    /// </summary>
    public struct EnemyBulletTag : IComponentData
    {
    }
}
