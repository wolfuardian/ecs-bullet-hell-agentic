using UnityEngine;
using Unity.Entities;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// Authoring component for the wave spawning singleton.
    /// Attach to a GameObject in the scene to enable wave-based enemy spawning.
    /// Bakes into a WaveData singleton entity.
    /// </summary>
    public class WaveAuthoring : MonoBehaviour
    {
        [Header("Wave Timing")]
        [SerializeField]
        [Tooltip("Seconds between waves (rest period)")]
        private float _waveInterval = 10f;

        [SerializeField]
        [Tooltip("Initial countdown before the first wave begins")]
        private float _firstWaveDelay = 3f;

        [Header("Spawning")]
        [SerializeField]
        [Tooltip("Base number of enemies per wave (scales: base + (wave-1)*2)")]
        private int _enemiesPerWave = 3;

        [SerializeField]
        [Tooltip("Seconds between individual enemy spawns within a wave")]
        private float _spawnInterval = 0.5f;

        public class Baker : Baker<WaveAuthoring>
        {
            public override void Bake(WaveAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new Wave.WaveData
                {
                    CurrentWave = 0,
                    WaveTimer = authoring._firstWaveDelay,
                    WaveInterval = authoring._waveInterval,
                    EnemiesPerWave = authoring._enemiesPerWave,
                    EnemiesSpawnedThisWave = 0,
                    WaveActive = false,
                    SpawnTimer = 0f,
                    SpawnInterval = authoring._spawnInterval
                });
            }
        }
    }
}
