using Unity.Entities;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 標記敵人 Entity 的空 Component。
    /// 用於 System query 區分敵人與其他 Entity。
    /// </summary>
    public struct EnemyTag : IComponentData
    {
    }
}
