using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;
using MyGame.ECS.Danmaku;

namespace MyGame.ECS.Wave
{
    /// <summary>
    /// Manages wave-based enemy spawning. Each wave spawns a set number of enemies
    /// with configurable DanmakuPattern. Effectively replaces the periodic
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
                // Between waves -- count down
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

            // Wave is active -- spawn enemies at intervals
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
            var rng = Random.CreateFromIndex(seed);
            var spawnX = rng.NextFloat(spawnerData.SpawnMinX, spawnerData.SpawnMaxX);
            var spawnPos = new float3(spawnX, spawnerData.SpawnY, 0f);

            // Instantiate enemy
            var enemy = ecb.Instantiate(spawnerData.Prefab);
            ecb.SetComponent(enemy, LocalTransform.FromPosition(spawnPos));

            // Assign danmaku pattern based on wave number
            var patternData = AssignPattern(wave.CurrentWave, rng);
            ecb.AddComponent(enemy, patternData);

            // Spiral enemies need DanmakuSpiralAngle tracker
            if (patternData.PatternType == DanmakuPatternType.Spiral)
            {
                ecb.AddComponent(enemy, new DanmakuSpiralAngle { Value = 0f });
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
        /// Assigns a DanmakuPattern based on the current wave number.
        /// Wave 1-2: Straight (Pellet, White/Red).
        /// Wave 3-4: Straight or Fan (BallS/RiceS, Red/Blue/Green).
        /// Wave 5+: All patterns with wider shape/color variety.
        /// </summary>
        private static DanmakuPattern AssignPattern(int currentWave, Random rng)
        {
            var pattern = new DanmakuPattern
            {
                PatternType = DanmakuPatternType.Straight,
                Shape = BulletShape.Pellet,
                Color = BulletColor.White,
                Speed = 8f,
                BulletCount = 1,
                SpreadAngle = 1.047f, // ~60 degrees
                SpiralSpeed = 0.262f, // ~15 degrees
                Accel = 0f,
                MaxSpeed = 0f,
                SpawnDelayFrames = 0
            };

            // Scale speed with wave
            pattern.Speed = 8f + (currentWave - 1) * 0.5f;

            if (currentWave <= 2)
            {
                // Wave 1-2: Straight, Pellet, White/Red
                pattern.PatternType = DanmakuPatternType.Straight;
                pattern.Shape = BulletShape.Pellet;
                pattern.Color = rng.NextInt(0, 2) == 0
                    ? BulletColor.White
                    : BulletColor.Red;
            }
            else if (currentWave <= 4)
            {
                // Wave 3-4: Straight or Fan, BallS/RiceS, Red/Blue/Green
                int roll = rng.NextInt(0, 2);
                if (roll == 0)
                {
                    pattern.PatternType = DanmakuPatternType.Straight;
                }
                else
                {
                    pattern.PatternType = DanmakuPatternType.Fan;
                    pattern.BulletCount = 3;
                    pattern.SpreadAngle = 1.047f;
                }

                pattern.Shape = rng.NextInt(0, 2) == 0
                    ? BulletShape.BallS
                    : BulletShape.RiceS;

                int colorRoll = rng.NextInt(0, 3);
                pattern.Color = colorRoll == 0 ? BulletColor.Red
                    : colorRoll == 1 ? BulletColor.Blue
                    : BulletColor.Green;
            }
            else
            {
                // Wave 5+: all patterns with wider variety
                int roll = rng.NextInt(0, 6);
                pattern.PatternType = (DanmakuPatternType)roll;

                switch (pattern.PatternType)
                {
                    case DanmakuPatternType.Fan:
                        pattern.BulletCount = 3 + (currentWave - 5);
                        pattern.SpreadAngle = 1.047f;
                        break;

                    case DanmakuPatternType.Spiral:
                        pattern.SpiralSpeed = 0.262f + (currentWave - 5) * 0.087f;
                        break;

                    case DanmakuPatternType.Ring:
                        pattern.BulletCount = 8 + (currentWave - 5) * 2;
                        break;

                    case DanmakuPatternType.Spread:
                        pattern.BulletCount = 5 + (currentWave - 5);
                        pattern.SpreadAngle = 0.524f; // ~30 degrees
                        break;
                }

                // Wider shape variety for wave 5+
                int shapeRoll = rng.NextInt(0, 5);
                pattern.Shape = shapeRoll switch
                {
                    0 => BulletShape.Kunai,
                    1 => BulletShape.Scale,
                    2 => BulletShape.Ofuda,
                    3 => BulletShape.StarS,
                    _ => BulletShape.BallM,
                };

                // Wider color variety
                int colorRoll = rng.NextInt(0, 6);
                pattern.Color = colorRoll switch
                {
                    0 => BulletColor.Red,
                    1 => BulletColor.Blue,
                    2 => BulletColor.Green,
                    3 => BulletColor.Purple,
                    4 => BulletColor.Yellow,
                    _ => BulletColor.Cyan,
                };
            }

            return pattern;
        }
    }
}
