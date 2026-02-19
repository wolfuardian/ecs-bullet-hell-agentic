using Unity.Entities;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 持有敵人子彈 Prefab Entity 的參考，掛在敵人 Entity 上。
    /// EnemyBulletSpawnSystem 用此 Prefab 來 Instantiate 敵彈。
    /// </summary>
    public struct EnemyBulletPrefabRef : IComponentData
    {
        public Entity Value;
    }
}
