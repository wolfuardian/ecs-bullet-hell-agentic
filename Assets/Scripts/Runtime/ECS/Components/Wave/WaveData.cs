using Unity.Entities;

namespace MyGame.ECS.Wave
{
    /// <summary>
    /// Singleton component controlling wave-based enemy spawning.
    /// Manages wave progression, timing, and per-wave enemy count.
    /// </summary>
    public struct WaveData : IComponentData
    {
        /// <summary>Current wave number (starts at 1 when first wave begins).</summary>
        public int CurrentWave;

        /// <summary>Countdown timer to the next wave (seconds).</summary>
        public float WaveTimer;

        /// <summary>Seconds between waves (rest period).</summary>
        public float WaveInterval;

        /// <summary>Base number of enemies per wave (scales with wave number).</summary>
        public int EnemiesPerWave;

        /// <summary>How many enemies have been spawned in the current wave.</summary>
        public int EnemiesSpawnedThisWave;

        /// <summary>True while the system is actively spawning enemies for a wave.</summary>
        public bool WaveActive;

        /// <summary>Timer between individual enemy spawns within a wave.</summary>
        public float SpawnTimer;

        /// <summary>Seconds between individual spawns within a wave.</summary>
        public float SpawnInterval;
    }
}
