using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Difficulty
{
    /// <summary>
    /// Scales game difficulty over time by increasing the spawn rate multiplier
    /// and reducing the enemy spawn interval accordingly.
    /// Runs before EnemySpawnSystem so adjusted intervals take effect immediately.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(Enemy.EnemySpawnSystem))]
    public partial struct DifficultySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DifficultyData>();
            state.RequireForUpdate<Enemy.EnemySpawnerData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var difficulty in
                SystemAPI.Query<RefRW<DifficultyData>>())
            {
                difficulty.ValueRW.ElapsedTime += dt;

                difficulty.ValueRW.SpawnRateMultiplier = math.min(
                    1f + (difficulty.ValueRO.ElapsedTime / difficulty.ValueRO.ScalingInterval),
                    difficulty.ValueRO.MaxMultiplier);

                foreach (var spawner in
                    SystemAPI.Query<RefRW<Enemy.EnemySpawnerData>>())
                {
                    spawner.ValueRW.Interval =
                        difficulty.ValueRO.BaseSpawnInterval / difficulty.ValueRO.SpawnRateMultiplier;
                }
            }
        }
    }
}
