using Unity.Entities;

namespace MyGame.ECS.Difficulty
{
    /// <summary>
    /// Singleton — controls difficulty scaling over time.
    /// Increases spawn rate multiplier as elapsed time grows.
    /// </summary>
    public struct DifficultyData : IComponentData
    {
        /// <summary>Total game time in seconds since difficulty tracking started.</summary>
        public float ElapsedTime;

        /// <summary>Current spawn rate multiplier (starts at 1.0).</summary>
        public float SpawnRateMultiplier;

        /// <summary>Maximum allowed multiplier cap.</summary>
        public float MaxMultiplier;

        /// <summary>Seconds per 1x multiplier increase (e.g. 30 = +1x every 30s).</summary>
        public float ScalingInterval;

        /// <summary>Snapshot of the original EnemySpawnerData.Interval value.</summary>
        public float BaseSpawnInterval;
    }
}
