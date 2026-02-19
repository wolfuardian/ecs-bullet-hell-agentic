using Unity.Entities;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// 掛在玩家 Entity 上，記錄發射子彈的速度。
    /// BulletSpawnSystem 讀取此值設定子彈 Velocity。
    /// </summary>
    public struct BulletSpeedData : IComponentData
    {
        public float Value;
    }
}
