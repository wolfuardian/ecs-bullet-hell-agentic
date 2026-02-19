using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 根據 EnemyVelocity 移動所有敵人 Entity。
    /// 與 BulletMovementSystem 分離，使用 EnemyTag + EnemyVelocity query。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, velocity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<EnemyVelocity>>()
                    .WithAll<EnemyTag>())
            {
                transform.ValueRW.Position += velocity.ValueRO.Value * dt;
            }
        }
    }
}
