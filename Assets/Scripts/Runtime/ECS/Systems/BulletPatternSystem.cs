using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;
using MyGame.ECS.Enemy;

namespace MyGame.ECS.Wave
{
    /// <summary>
    /// [DEPRECATED] Use DanmakuPatternSystem instead.
    /// Legacy pattern system for enemies with BulletPatternData.
    /// Kept temporarily for backward compatibility.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemyBulletSpawnSystem))]
    public partial struct BulletPatternSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BulletPatternData>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Find player position for AIMED pattern
            var playerPos = float3.zero;
            bool hasPlayer = false;
            foreach (var playerTransform in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<Player.PlayerTag>())
            {
                playerPos = playerTransform.ValueRO.Position;
                hasPlayer = true;
                break;
            }

            foreach (var (transform, prefabRef, cooldown, bulletSpeed, pattern) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyBulletPrefabRef>,
                    RefRW<EnemyShootCooldown>, RefRO<EnemyBulletSpeedData>,
                    RefRO<BulletPatternData>>()
                    .WithAll<EnemyTag>())
            {
                // Decrement cooldown
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRO.Timer > 0f)
                    continue;

                // Reset cooldown (prevents EnemyBulletSpawnSystem from double-firing)
                cooldown.ValueRW.Timer = cooldown.ValueRO.Duration;

                var enemyPos = transform.ValueRO.Position;
                var spawnPos = enemyPos + new float3(0f, -0.5f, 0f);
                var speed = bulletSpeed.ValueRO.Value;
                var prefab = prefabRef.ValueRO.Value;

                switch (pattern.ValueRO.PatternType)
                {
                    case BulletPatternData.STRAIGHT:
                        SpawnBullet(ref ecb, prefab, spawnPos,
                            new float3(0f, -speed, 0f));
                        break;

                    case BulletPatternData.FAN:
                        SpawnFanBullets(ref ecb, prefab, spawnPos, speed,
                            pattern.ValueRO.BulletCount, pattern.ValueRO.SpreadAngle);
                        break;

                    case BulletPatternData.SPIRAL:
                        // Spiral handled in separate loop below (needs RefRW<SpiralAngle>)
                        break;

                    case BulletPatternData.AIMED:
                        if (hasPlayer)
                        {
                            var direction = math.normalize(playerPos - enemyPos);
                            SpawnBullet(ref ecb, prefab, spawnPos, speed * direction);
                        }
                        else
                        {
                            // No player, fall back to straight down
                            SpawnBullet(ref ecb, prefab, spawnPos,
                                new float3(0f, -speed, 0f));
                        }
                        break;
                }
            }

            // Second pass for SPIRAL enemies (need RefRW<SpiralAngle>)
            foreach (var (transform, prefabRef, cooldown, bulletSpeed, pattern, spiralAngle) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyBulletPrefabRef>,
                    RefRO<EnemyShootCooldown>, RefRO<EnemyBulletSpeedData>,
                    RefRO<BulletPatternData>, RefRW<SpiralAngle>>()
                    .WithAll<EnemyTag>())
            {
                // Only process if cooldown was just reset (timer == Duration means just fired)
                if (math.abs(cooldown.ValueRO.Timer - cooldown.ValueRO.Duration) > 0.001f)
                    continue;

                if (pattern.ValueRO.PatternType != BulletPatternData.SPIRAL)
                    continue;

                var speed = bulletSpeed.ValueRO.Value;
                var spawnPos = transform.ValueRO.Position + new float3(0f, -0.5f, 0f);
                var prefab = prefabRef.ValueRO.Value;

                var angleRad = math.radians(spiralAngle.ValueRO.Value);
                var vel = speed * new float3(math.cos(angleRad), math.sin(angleRad), 0f);
                SpawnBullet(ref ecb, prefab, spawnPos, vel);

                // Increment spiral angle
                spiralAngle.ValueRW.Value += pattern.ValueRO.SpiralSpeed;
            }
        }

        /// <summary>
        /// Spawns a single bullet entity with the given position and velocity.
        /// </summary>
        private static void SpawnBullet(
            ref EntityCommandBuffer ecb, Entity prefab, float3 position, float3 velocity)
        {
            var bullet = ecb.Instantiate(prefab);
            ecb.SetComponent(bullet, LocalTransform.FromPosition(position));
            ecb.SetComponent(bullet, new Velocity { Value = velocity });
            ecb.AddComponent<EnemyBulletTag>(bullet);
        }

        /// <summary>
        /// Spawns multiple bullets in a fan pattern.
        /// </summary>
        private static void SpawnFanBullets(
            ref EntityCommandBuffer ecb, Entity prefab, float3 spawnPos,
            float speed, int bulletCount, float spreadAngleDeg)
        {
            if (bulletCount <= 0)
                return;

            // Center angle points down (-90 degrees = -PI/2)
            const float CENTER_ANGLE_DEG = -90f;

            if (bulletCount == 1)
            {
                var angleRad = math.radians(CENTER_ANGLE_DEG);
                var vel = speed * new float3(math.cos(angleRad), math.sin(angleRad), 0f);
                SpawnBullet(ref ecb, prefab, spawnPos, vel);
                return;
            }

            float angleStep = spreadAngleDeg / (bulletCount - 1);
            float startAngle = CENTER_ANGLE_DEG - spreadAngleDeg * 0.5f;

            for (int i = 0; i < bulletCount; i++)
            {
                float angleDeg = startAngle + i * angleStep;
                float angleRad = math.radians(angleDeg);
                var vel = speed * new float3(math.cos(angleRad), math.sin(angleRad), 0f);
                SpawnBullet(ref ecb, prefab, spawnPos, vel);
            }
        }
    }
}
