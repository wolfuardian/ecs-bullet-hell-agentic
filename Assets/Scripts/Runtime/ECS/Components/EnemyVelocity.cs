using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 敵人的移動速度向量（世界空間，每秒單位）。
    /// 與子彈的 Velocity 分離，避免 BulletMovementSystem 誤處理敵人。
    /// </summary>
    public struct EnemyVelocity : IComponentData
    {
        public float3 Value;
    }
}
