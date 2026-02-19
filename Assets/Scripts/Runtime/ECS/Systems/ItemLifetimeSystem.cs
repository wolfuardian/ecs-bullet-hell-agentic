using Unity.Burst;
using Unity.Entities;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Decrements ItemLifetime each frame and destroys items when lifetime expires.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ItemLifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ItemTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (lifetime, entity) in
                SystemAPI.Query<RefRW<ItemLifetime>>()
                    .WithAll<ItemTag>()
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
