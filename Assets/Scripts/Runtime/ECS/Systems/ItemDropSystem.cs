using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Spawns item entities when enemies with ItemDropData die.
    /// Runs after EnemyPlayerCollisionSystem and before DeathSystem
    /// so that dead enemies still exist for querying.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyPlayerCollisionSystem))]
    [UpdateBefore(typeof(DeathSystem))]
    public partial struct ItemDropSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DeadTag>();
            state.RequireForUpdate<ItemPrefabRef>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var itemPrefabRef = SystemAPI.GetSingleton<ItemPrefabRef>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var elapsedTime = SystemAPI.Time.ElapsedTime;
            int entityIndex = 0;

            foreach (var (transform, dropData, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<ItemDropData>>()
                    .WithAll<DeadTag>()
                    .WithEntityAccess())
            {
                // Time-based seed combined with entity index for uniqueness
                var seed = (uint)((elapsedTime + 1.0) * 10000.0 + entityIndex) | 1u;
                var rng = Unity.Mathematics.Random.CreateFromIndex(seed);
                entityIndex++;

                if (rng.NextFloat() > dropData.ValueRO.DropChance)
                    continue;

                var itemEntity = ecb.Instantiate(itemPrefabRef.Prefab);
                ecb.SetComponent(itemEntity, LocalTransform.FromPosition(transform.ValueRO.Position));
                ecb.AddComponent<ItemTag>(itemEntity);
                ecb.AddComponent(itemEntity, new ItemData
                {
                    Type = dropData.ValueRO.DropType,
                    ScoreValue = 100,
                    PowerValue = 1
                });
                ecb.AddComponent(itemEntity, new ItemVelocity
                {
                    Value = new float3(0f, -2f, 0f)
                });
                ecb.AddComponent(itemEntity, new CollisionRadius
                {
                    Value = 0.2f
                });
                ecb.AddComponent(itemEntity, new ItemLifetime
                {
                    Value = 10f
                });
            }
        }
    }
}
