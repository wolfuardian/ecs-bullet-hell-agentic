using Unity.Burst;
using Unity.Entities;

namespace MyGame.ECS.Bullet
{
    /// <summary>
    /// 遞減子彈存活時間，歸零時用 ECB 銷毀 Entity。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    public partial struct BulletLifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BulletTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (lifetime, entity) in
                SystemAPI.Query<RefRW<BulletLifetime>>()
                    .WithAll<BulletTag>()
                    .WithEntityAccess())
            {
                lifetime.ValueRW.Value -= dt;

                if (lifetime.ValueRO.Value <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
