using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;

namespace MyGame.ECS.Wave
{
    /// <summary>
    /// Manages wave-based enemy spawning. Each wave spawns a set number of enemies
    /// with configurable bullet patterns. Effectively replaces the periodic
    /// EnemySpawnSystem by setting its interval very high.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemySpawnSystem))]
    public partial struct WaveSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WaveData>();
            state.RequireForUpdate<EnemySpawnerData>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            var waveRW = SystemAPI.GetSingletonRW<WaveData>();
            ref var wave = ref waveRW.ValueRW;

            // Disable old periodic spawner by setting a very high interval
            foreach (var spawner in SystemAPI.Query<RefRW<EnemySpawnerData>>())
            {
                spawner.ValueRW.Interval = 99999f;
                spawner.ValueRW.Timer = 99999f;
            }

            // Read spawner data for prefab and spawn bounds
            var spawnerData = SystemAPI.GetSingleton<EnemySpawnerData>();

            if (!wave.WaveActive)
            {
                // Between waves — count down
                wave.WaveTimer -= dt;
                if (wave.WaveTimer <= 0f)
                {
                    // Start a new wave
                    wave.WaveActive = true;
                    wave.CurrentWave++;
                    wave.EnemiesSpawnedThisWave = 0;
                    wave.SpawnTimer = 0f; // spawn first enemy immediately
                }
                return;
            }

            // Wave is active — spawn enemies at intervals
            wave.SpawnTimer -= dt;
            if (wave.SpawnTimer > 0f)
                return;

            // Reset spawn timer for next enemy
            wave.SpawnTimer = wave.SpawnInterval;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Calculate total enemies for this wave: base + (wave - 1) * 2
            int totalEnemies = wave.EnemiesPerWave + (wave.CurrentWave - 1) * 2;

            // Random position
            var seed = (uint)((SystemAPI.Time.ElapsedTime + 1.0) * 10000.0 + wave.EnemiesSpawnedThisWave) | 1u;
            var rng = Unity.Mathematics.Random.CreateFromIndex(seed);
            var spawnX = rng.NextFloat(spawnerData.SpawnMinX, spawnerData.SpawnMaxX);
            var spawnPos = new float3(spawnX, spawnerData.SpawnY, 0f);

            // Instantiate enemy
            var enemy = ecb.Instantiate(spawnerData.Prefab);
            ecb.SetComponent(enemy, LocalTransform.FromPosition(spawnPos));

            // Assign bullet pattern based on wave number
            var patternData = AssignPattern(wave.CurrentWave, rng);
            ecb.AddComponent(enemy, patternData);

            // SPIRAL enemies need SpiralAngle tracker
            if (patternData.PatternType == BulletPatternData.SPIRAL)
            {
                ecb.AddComponent(enemy, new SpiralAngle { Value = 0f });
            }

            wave.EnemiesSpawnedThisWave++;

            // Check if wave is complete
            if (wave.EnemiesSpawnedThisWave >= totalEnemies)
            {
                wave.WaveActive = false;
                wave.WaveTimer = wave.WaveInterval;
            }
        }

        /// <summary>
        /// Assigns a bullet pattern based on the current wave number.
        /// Wave 1-2: STRAIGHT only. Wave 3-4: STRAIGHT or FAN. Wave 5+: all patterns.
        /// </summary>
        private static BulletPatternData AssignPattern(int currentWave, Unity.Mathematics.Random rng)
        {
            var pattern = new BulletPatternData
            {
                PatternType = BulletPatternData.STRAIGHT,
                BulletCount = 1,
                SpreadAngle = 60f,
                SpiralSpeed = 15f
            };

            if (currentWave <= 2)
            {
                // All STRAIGHT
                pattern.PatternType = BulletPatternData.STRAIGHT;
            }
            else if (currentWave <= 4)
            {
                // Mix of STRAIGHT and FAN
                int roll = rng.NextInt(0, 2); // 0 or 1
                if (roll == 0)
                {
                    pattern.PatternType = BulletPatternData.STRAIGHT;
                }
                else
                {
                    pattern.PatternType = BulletPatternData.FAN;
                    pattern.BulletCount = 3;
                    pattern.SpreadAngle = 60f;
                }
            }
            else
            {
                // Wave 5+: all patterns
                int roll = rng.NextInt(0, 4); // 0..3
                pattern.PatternType = roll;
                if (roll == BulletPatternData.FAN)
                {
                    pattern.BulletCount = 3 + (currentWave - 5); // scales with wave
                    pattern.SpreadAngle = 60f;
                }
                else if (roll == BulletPatternData.SPIRAL)
                {
                    pattern.SpiralSpeed = 15f + (currentWave - 5) * 5f;
                }
            }

            return pattern;
        }
    }
}
