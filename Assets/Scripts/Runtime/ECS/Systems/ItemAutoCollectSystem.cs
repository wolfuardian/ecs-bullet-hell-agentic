using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;
using MyGame.ECS.Player;
using MyGame.ECS.Bomb;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// When the player has an active bomb, all items are attracted toward
    /// the player at high speed, allowing automatic collection by ItemCollectionSystem.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ItemCollectionSystem))]
    public partial struct ItemAutoCollectSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<ItemTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Find player with active bomb
            float3 playerPos = default;
            bool bombActive = false;

            foreach (var (transform, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<PlayerTag, BombActiveData>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                playerPos = transform.ValueRO.Position;
                bombActive = true;
                break;
            }

            if (!bombActive)
                return;

            // Redirect all items toward the player
            foreach (var (transform, velocity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<ItemVelocity>>()
                    .WithAll<ItemTag>())
            {
                var direction = playerPos - transform.ValueRO.Position;
                var dist = math.length(direction);
                if (dist > 0.001f)
                {
                    velocity.ValueRW.Value = math.normalize(direction) * 20f;
                }
            }
        }
    }
}
