using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Fires bullets for enemies that have a DanmakuPattern component.
    /// Supports Straight, Fan, Spiral, Aimed, Ring, and Spread patterns
    /// using BulletFactory to create danmaku bullets with BulletMotion.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemyBulletSpawnSystem))]
    public partial struct DanmakuPatternSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DanmakuPattern>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        // Note: OnUpdate is NOT [BurstCompile] because BulletFactory calls
        // BulletShapeTable.Get() which accesses a managed static array.
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Find player position for AIMED patterns
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

            // Main pass: all patterns except Spiral
            foreach (var (transform, cooldown, pattern) in
                SystemAPI.Query<RefRO<LocalTransform>,
                    RefRW<EnemyShootCooldown>,
                    RefRO<DanmakuPattern>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DanmakuSpiralAngle>())
            {
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRO.Timer > 0f)
                    continue;

                cooldown.ValueRW.Timer = cooldown.ValueRO.Duration;

                var p = pattern.ValueRO;
                var spawnPos = transform.ValueRO.Position + new float3(0f, -0.5f, 0f);

                // Down angle: -PI/2 radians
                const float DOWN_ANGLE = -math.PI * 0.5f;

                switch (p.PatternType)
                {
                    case DanmakuPatternType.Straight:
                        BulletFactory.Shot(ref ecb, spawnPos, p.Speed, DOWN_ANGLE,
                            p.Shape, p.Color, p.SpawnDelayFrames);
                        break;

                    case DanmakuPatternType.Fan:
                        BulletFactory.ShotFan(ref ecb, spawnPos, p.Speed,
                            DOWN_ANGLE, p.SpreadAngle, p.BulletCount,
                            p.Shape, p.Color, p.SpawnDelayFrames);
                        break;

                    case DanmakuPatternType.Aimed:
                        if (hasPlayer)
                        {
                            BulletFactory.ShotAim(ref ecb, spawnPos, p.Speed,
                                playerPos, p.Shape, p.Color, p.SpawnDelayFrames);
                        }
                        else
                        {
                            BulletFactory.Shot(ref ecb, spawnPos, p.Speed, DOWN_ANGLE,
                                p.Shape, p.Color, p.SpawnDelayFrames);
                        }
                        break;

                    case DanmakuPatternType.Ring:
                        BulletFactory.ShotRing(ref ecb, spawnPos, p.Speed,
                            DOWN_ANGLE, p.BulletCount,
                            p.Shape, p.Color, p.SpawnDelayFrames);
                        break;

                    case DanmakuPatternType.Spread:
                    {
                        var seed = (uint)((SystemAPI.Time.ElapsedTime + 1.0) * 10000.0) | 1u;
                        BulletFactory.ShotSpread(ref ecb, spawnPos, DOWN_ANGLE,
                            p.SpreadAngle, new float2(p.Speed * 0.5f, p.Speed * 1.5f),
                            p.BulletCount, p.Shape, p.Color, p.SpawnDelayFrames, seed);
                        break;
                    }

                    // Spiral handled in second pass
                    default:
                        break;
                }
            }

            // Second pass: Spiral enemies (need RefRW<DanmakuSpiralAngle>)
            foreach (var (transform, cooldown, pattern, spiralAngle) in
                SystemAPI.Query<RefRO<LocalTransform>,
                    RefRW<EnemyShootCooldown>,
                    RefRO<DanmakuPattern>,
                    RefRW<DanmakuSpiralAngle>>()
                    .WithAll<EnemyTag>())
            {
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRO.Timer > 0f)
                    continue;

                cooldown.ValueRW.Timer = cooldown.ValueRO.Duration;

                var p = pattern.ValueRO;
                if (p.PatternType != DanmakuPatternType.Spiral)
                    continue;

                var spawnPos = transform.ValueRO.Position + new float3(0f, -0.5f, 0f);

                BulletFactory.Shot(ref ecb, spawnPos, p.Speed,
                    spiralAngle.ValueRO.Value, p.Shape, p.Color, p.SpawnDelayFrames);

                // Increment spiral angle
                spiralAngle.ValueRW.Value += p.SpiralSpeed;
            }
        }
    }
}
