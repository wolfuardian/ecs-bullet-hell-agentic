using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace MyGame.ECS.Bullet
{
    /// <summary>
    /// 根據 Velocity 移動所有子彈 Entity。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BulletMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BulletTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, velocity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>()
                    .WithAll<BulletTag>())
            {
                transform.ValueRW.Position += velocity.ValueRO.Value * dt;
            }
        }
    }
}
