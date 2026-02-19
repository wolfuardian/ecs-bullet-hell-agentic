using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Player;
using MyGame.ECS.Danmaku;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Detects enemy bullet (EnemyBulletTag) vs player (PlayerTag) collision.
    /// Supports two bullet types:
    /// - Legacy bullets with CollisionRadius (circle-only)
    /// - New danmaku bullets with BulletHitbox (multi-shape)
    /// On hit: destroys bullet, damages player HP, starts invincibility, adds DeadTag if HP &lt;= 0.
    /// Skips all checks while player is invincible (InvincibilityTimer &gt; 0).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    [UpdateAfter(typeof(InvincibilitySystem))]
    public partial struct EnemyBulletCollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
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
            float playerR = 0f;
            int playerHp = 0;
            int playerMaxHp = 0;
            float invTimer = 0f;
            float invDuration = 0f;
            Entity playerEntity = Entity.Null;
            bool playerFound = false;

            foreach (var (transform, radius, health, invincibility, invDur, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>,
                    RefRO<HealthData>, RefRO<InvincibilityTimer>, RefRO<InvincibilityDuration>>()
                    .WithAll<PlayerTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                playerPos = transform.ValueRO.Position;
                playerR = radius.ValueRO.Value;
                playerHp = health.ValueRO.Current;
                playerMaxHp = health.ValueRO.Max;
                invTimer = invincibility.ValueRO.Value;
                invDuration = invDur.ValueRO.Value;
                playerEntity = entity;
                playerFound = true;
                break; // single player
            }

            if (!playerFound || invTimer > 0f)
                return;

            bool playerHit = false;
            int totalDamage = 0;

            // --- Loop 1: Legacy bullets with CollisionRadius (no BulletHitbox) ---
            foreach (var (bulletTransform, bulletRadius, bulletDmg, bulletEntity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>, RefRO<DamageOnContact>>()
                    .WithAll<BulletTag, EnemyBulletTag>()
                    .WithNone<DeadTag, BulletHitbox>()
                    .WithEntityAccess())
            {
                var bulletPos = bulletTransform.ValueRO.Position;
                var bulletR = bulletRadius.ValueRO.Value;
                var radiusSum = playerR + bulletR;
                var distSq = math.distancesq(playerPos, bulletPos);

                if (distSq <= radiusSum * radiusSum)
                {
                    ecb.DestroyEntity(bulletEntity);
                    totalDamage += bulletDmg.ValueRO.Value;
                    playerHit = true;
                    break;
                }
            }

            // --- Loop 2: Danmaku bullets with BulletHitbox ---
            if (!playerHit)
            {
                var playerPos2 = playerPos.xy;

                foreach (var (bulletTransform, hitbox, motion, bulletDmg, bulletEntity) in
                    SystemAPI.Query<RefRO<LocalTransform>, RefRO<BulletHitbox>,
                        RefRO<BulletMotion>, RefRO<DamageOnContact>>()
                        .WithAll<BulletTag, EnemyBulletTag>()
                        .WithNone<DeadTag, SpawnDelay>()
                        .WithEntityAccess())
                {
                    var bulletPos2 = bulletTransform.ValueRO.Position.xy;

                    if (HitboxCollisionUtils.TestHitbox(
                        playerPos2, playerR,
                        bulletPos2, hitbox.ValueRO, motion.ValueRO.Angle))
                    {
                        ecb.DestroyEntity(bulletEntity);
                        totalDamage += bulletDmg.ValueRO.Value;
                        playerHit = true;
                        break;
                    }
                }
            }

            if (playerHit)
            {
                var newHp = playerHp - totalDamage;
                ecb.SetComponent(playerEntity, new HealthData
                {
                    Current = newHp,
                    Max = playerMaxHp
                });

                if (newHp <= 0)
                {
                    ecb.AddComponent<DeadTag>(playerEntity);
                }
                else
                {
                    ecb.SetComponent(playerEntity, new InvincibilityTimer
                    {
                        Value = invDuration
                    });
                }
            }
        }
    }
}
