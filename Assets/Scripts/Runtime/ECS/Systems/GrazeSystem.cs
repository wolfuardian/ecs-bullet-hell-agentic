using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;
using MyGame.ECS.Player;

namespace MyGame.ECS.Graze
{
    /// <summary>
    /// Detects enemy bullets passing near the player within graze radius
    /// but outside collision radius. Increments GrazeData.Count and adds
    /// GrazedTag to the bullet to prevent double-counting.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InvincibilitySystem))]
    [UpdateBefore(typeof(EnemyBulletCollisionSystem))]
    public partial struct GrazeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<GrazeData>();
            state.RequireForUpdate<EnemyBulletTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Get player data (single player assumption)
            float3 playerPos = default;
            float playerCollisionR = 0f;
            float playerGrazeR = 0f;
            int grazeCount = 0;
            Entity playerEntity = Entity.Null;
            bool playerFound = false;

            foreach (var (transform, radius, grazeData, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>, RefRO<GrazeData>>()
                    .WithAll<PlayerTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                playerPos = transform.ValueRO.Position;
                playerCollisionR = radius.ValueRO.Value;
                playerGrazeR = grazeData.ValueRO.GrazeRadius;
                grazeCount = grazeData.ValueRO.Count;
                playerEntity = entity;
                playerFound = true;
                break; // single player
            }

            if (!playerFound)
                return;

            foreach (var (bulletTransform, bulletRadius, bulletEntity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>>()
                    .WithAll<BulletTag, EnemyBulletTag>()
                    .WithNone<DeadTag, GrazedTag>()
                    .WithEntityAccess())
            {
                var bulletPos = bulletTransform.ValueRO.Position;
                var bulletR = bulletRadius.ValueRO.Value;
                var collisionRadiusSum = playerCollisionR + bulletR;
                var grazeRadiusSum = playerGrazeR + bulletR;
                var distSq = math.distancesq(playerPos, bulletPos);

                if (distSq > collisionRadiusSum * collisionRadiusSum &&
                    distSq <= grazeRadiusSum * grazeRadiusSum)
                {
                    grazeCount++;
                    ecb.AddComponent<GrazedTag>(bulletEntity);
                }
            }

            // Write back updated graze count
            ecb.SetComponent(playerEntity, new GrazeData
            {
                Count = grazeCount,
                GrazeRadius = playerGrazeR
            });
        }
    }
}
