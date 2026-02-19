using Unity.Burst;
using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Decrements SpawnDelay.FramesRemaining each frame.
    /// Removes the SpawnDelay component when it reaches 0,
    /// activating the bullet for rendering and collision.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct SpawnDelaySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnDelay>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (delay, entity) in
                SystemAPI.Query<RefRW<SpawnDelay>>()
                    .WithEntityAccess())
            {
                delay.ValueRW.FramesRemaining--;

                if (delay.ValueRO.FramesRemaining <= 0)
                {
                    ecb.RemoveComponent<SpawnDelay>(entity);
                }
            }
        }
    }
}
