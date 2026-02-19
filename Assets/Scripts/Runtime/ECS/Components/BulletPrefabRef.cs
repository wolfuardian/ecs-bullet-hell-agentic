using Unity.Entities;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// 持有子彈 Prefab Entity 的參考，掛在玩家 Entity 上。
    /// BulletSpawnSystem 用此 Prefab 來 Instantiate 子彈。
    /// </summary>
    public struct BulletPrefabRef : IComponentData
    {
        public Entity Value;
    }
}
