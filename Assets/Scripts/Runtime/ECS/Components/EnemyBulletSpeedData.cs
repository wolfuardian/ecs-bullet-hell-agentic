using Unity.Entities;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// [DEPRECATED] Legacy bullet speed data.
    /// New enemies use DanmakuPattern.Speed instead.
    /// Kept for backward compatibility with EnemyBulletSpawnSystem.
    /// </summary>
    public struct EnemyBulletSpeedData : IComponentData
    {
        public float Value;
    }
}
