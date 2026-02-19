using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Moves item entities by their ItemVelocity each frame.
    /// Separate from BulletMovementSystem to avoid requiring BulletTag.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ItemMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ItemTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, velocity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<ItemVelocity>>()
                    .WithAll<ItemTag>())
            {
                transform.ValueRW.Position += velocity.ValueRO.Value * dt;
            }
        }
    }
}
