using Unity.Entities;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// [DEPRECATED] Legacy prefab-based bullet reference.
    /// New enemies use DanmakuPattern + BulletFactory instead.
    /// Kept for backward compatibility with EnemyBulletSpawnSystem.
    /// </summary>
    public struct EnemyBulletPrefabRef : IComponentData
    {
        public Entity Value;
    }
}
