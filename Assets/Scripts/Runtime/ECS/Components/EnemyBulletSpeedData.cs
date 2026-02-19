using Unity.Entities;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 掛在敵人 Entity 上，記錄發射子彈的速度（純量，方向由系統決定）。
    /// </summary>
    public struct EnemyBulletSpeedData : IComponentData
    {
        public float Value;
    }
}
