using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;
using MyGame.ECS.Player;
using MyGame.ECS.Score;
using MyGame.ECS.Bomb;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Detects collision between player and items. On collection:
    /// SCORE_ITEM increases score, POWER_ITEM increases power level,
    /// BOMB_ITEM increases bomb stock. Item is destroyed after collection.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InvincibilitySystem))]
    [UpdateBefore(typeof(EnemyBulletCollisionSystem))]
    public partial struct ItemCollectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<ItemTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get player data
            float3 playerPos = default;
            float playerR = 0f;
            Entity playerEntity = Entity.Null;
            bool playerFound = false;

            foreach (var (transform, radius, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>>()
                    .WithAll<PlayerTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                playerPos = transform.ValueRO.Position;
                playerR = radius.ValueRO.Value;
                playerEntity = entity;
                playerFound = true;
                break;
            }

            if (!playerFound)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Collect items into temp arrays for safe iteration
            var itemQuery = SystemAPI.QueryBuilder()
                .WithAll<ItemTag, LocalTransform, CollisionRadius, ItemData>()
                .Build();

            var itemEntities = itemQuery.ToEntityArray(Allocator.Temp);
            var itemTransforms = itemQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var itemRadii = itemQuery.ToComponentDataArray<CollisionRadius>(Allocator.Temp);
            var itemDatas = itemQuery.ToComponentDataArray<ItemData>(Allocator.Temp);

            for (int i = 0; i < itemEntities.Length; i++)
            {
                var itemPos = itemTransforms[i].Position;
                var itemR = itemRadii[i].Value;
                var radiusSum = playerR + itemR;
                var distSq = math.distancesq(playerPos, itemPos);

                if (distSq <= radiusSum * radiusSum)
                {
                    var itemData = itemDatas[i];

                    switch (itemData.Type)
                    {
                        case ItemData.SCORE_ITEM:
                            if (SystemAPI.HasSingleton<ScoreData>())
                            {
                                var score = SystemAPI.GetSingleton<ScoreData>();
                                score.Value += itemData.ScoreValue;
                                SystemAPI.SetSingleton(score);
                            }
                            break;

                        case ItemData.POWER_ITEM:
                            if (state.EntityManager.HasComponent<PowerLevelData>(playerEntity))
                            {
                                var power = state.EntityManager.GetComponentData<PowerLevelData>(playerEntity);
                                power.Level = math.min(power.Level + itemData.PowerValue, power.MaxLevel);
                                ecb.SetComponent(playerEntity, power);
                            }
                            break;

                        case ItemData.BOMB_ITEM:
                            if (state.EntityManager.HasComponent<BombData>(playerEntity))
                            {
                                var bomb = state.EntityManager.GetComponentData<BombData>(playerEntity);
                                bomb.Stock += 1;
                                ecb.SetComponent(playerEntity, bomb);
                            }
                            break;
                    }

                    ecb.DestroyEntity(itemEntities[i]);
                }
            }

            itemEntities.Dispose();
            itemTransforms.Dispose();
            itemRadii.Dispose();
            itemDatas.Dispose();
        }
    }
}
