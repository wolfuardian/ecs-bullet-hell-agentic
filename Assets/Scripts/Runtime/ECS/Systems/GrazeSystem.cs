using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;
using MyGame.ECS.Player;
using MyGame.ECS.Danmaku;

namespace MyGame.ECS.Graze
{
    /// <summary>
    /// Detects enemy bullets passing near the player within graze radius
    /// but outside collision radius. Increments GrazeData.Count and adds
    /// GrazedTag to the bullet to prevent double-counting.
    /// Supports both legacy CollisionRadius bullets and new BulletHitbox bullets.
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

            // --- Loop 1: Legacy bullets with CollisionRadius (no BulletHitbox) ---
            foreach (var (bulletTransform, bulletRadius, bulletEntity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>>()
                    .WithAll<BulletTag, EnemyBulletTag>()
                    .WithNone<DeadTag, GrazedTag, BulletHitbox>()
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

            // --- Loop 2: Danmaku bullets with BulletHitbox ---
            {
                var playerPos2 = playerPos.xy;

                foreach (var (bulletTransform, hitbox, motion, bulletEntity) in
                    SystemAPI.Query<RefRO<LocalTransform>, RefRO<BulletHitbox>, RefRO<BulletMotion>>()
                        .WithAll<BulletTag, EnemyBulletTag>()
                        .WithNone<DeadTag, GrazedTag, SpawnDelay>()
                        .WithEntityAccess())
                {
                    var bulletPos2 = bulletTransform.ValueRO.Position.xy;
                    var bulletAngle = motion.ValueRO.Angle;

                    // Use the largest dimension of the hitbox as effective radius
                    var effectiveR = math.max(hitbox.ValueRO.Size.x, hitbox.ValueRO.Size.y);
                    var collisionRadiusSum = playerCollisionR + effectiveR;
                    var grazeRadiusSum = playerGrazeR + effectiveR;
                    var distSq = math.distancesq(playerPos2, bulletPos2);

                    // Graze zone: outside collision range but inside graze range
                    if (distSq > collisionRadiusSum * collisionRadiusSum &&
                        distSq <= grazeRadiusSum * grazeRadiusSum)
                    {
                        grazeCount++;
                        ecb.AddComponent<GrazedTag>(bulletEntity);
                    }
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
