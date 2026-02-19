using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;
using MyGame.ECS.Wave;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for WaveSpawnSystem.
    /// Validates wave timing, progression, enemy spawning, and wave completion.
    /// </summary>
    [TestFixture]
    public class WaveSpawnSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _waveSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            _waveSystemHandle = _world.GetOrCreateSystem<WaveSpawnSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        /// <summary>
        /// Creates the WaveData singleton entity with default test values.
        /// </summary>
        private Entity CreateWaveData(
            float waveTimer = 3f,
            float waveInterval = 10f,
            int enemiesPerWave = 3,
            float spawnInterval = 0.5f,
            bool waveActive = false,
            int currentWave = 0)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new WaveData
            {
                CurrentWave = currentWave,
                WaveTimer = waveTimer,
                WaveInterval = waveInterval,
                EnemiesPerWave = enemiesPerWave,
                EnemiesSpawnedThisWave = 0,
                WaveActive = waveActive,
                SpawnTimer = 0f,
                SpawnInterval = spawnInterval
            });
            return entity;
        }

        /// <summary>
        /// Creates the EnemySpawnerData singleton with a valid prefab entity.
        /// </summary>
        private Entity CreateSpawnerData()
        {
            // Create a prefab-like entity for spawning
            var prefab = _em.CreateEntity();
            _em.AddComponentData(prefab, LocalTransform.FromPosition(float3.zero));
            _em.AddComponent<EnemyTag>(prefab);

            var spawner = _em.CreateEntity();
            _em.AddComponentData(spawner, new EnemySpawnerData
            {
                Prefab = prefab,
                Timer = 2f,
                Interval = 2f,
                SpawnMinX = -5f,
                SpawnMaxX = 5f,
                SpawnY = 6f
            });
            return spawner;
        }

        /// <summary>
        /// Advances time and updates the wave system + ECB playback.
        /// </summary>
        private void AdvanceTimeAndUpdate(float dt = 0f)
        {
            if (dt == 0f) dt = TEST_DELTA_TIME;
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + dt,
                deltaTime: dt));
            _waveSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void WaveTimer_Decrements()
        {
            // Arrange
            var waveEntity = CreateWaveData(waveTimer: 3f);
            CreateSpawnerData();

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var wave = _em.GetComponentData<WaveData>(waveEntity);
            Assert.Less(wave.WaveTimer, 3f,
                "WaveTimer should decrease each frame");
        }

        [Test]
        public void NewWave_StartsWhenTimerExpires()
        {
            // Arrange — set timer to very small value so it expires in one frame
            var waveEntity = CreateWaveData(waveTimer: 0.001f);
            CreateSpawnerData();

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var wave = _em.GetComponentData<WaveData>(waveEntity);
            Assert.IsTrue(wave.WaveActive || wave.EnemiesSpawnedThisWave > 0,
                "Wave should become active or have started spawning when timer expires");
            Assert.AreEqual(1, wave.CurrentWave,
                "CurrentWave should increment to 1 on first wave");
        }

        [Test]
        public void Enemies_SpawnedDuringWave()
        {
            // Arrange — start with wave already active
            var waveEntity = CreateWaveData(
                waveTimer: 0.001f,
                enemiesPerWave: 3,
                spawnInterval: 0f);
            CreateSpawnerData();

            // Act — advance enough frames to trigger spawning
            AdvanceTimeAndUpdate();
            AdvanceTimeAndUpdate();
            AdvanceTimeAndUpdate();

            // Assert — at least one enemy should have been spawned
            var wave = _em.GetComponentData<WaveData>(waveEntity);
            Assert.Greater(wave.EnemiesSpawnedThisWave, 0,
                "Should have spawned at least one enemy during active wave");
        }

        [Test]
        public void Wave_EndsAfterAllEnemiesSpawned()
        {
            // Arrange — wave 1: total = 3 + (1-1)*2 = 3 enemies
            // Start with wave about to begin
            var waveEntity = CreateWaveData(
                waveTimer: 0.001f,
                enemiesPerWave: 3,
                spawnInterval: 0.001f);
            CreateSpawnerData();

            // Act — advance many frames to spawn all enemies
            for (int i = 0; i < 20; i++)
            {
                AdvanceTimeAndUpdate();
            }

            // Assert
            var wave = _em.GetComponentData<WaveData>(waveEntity);
            Assert.IsFalse(wave.WaveActive,
                "Wave should end after all enemies are spawned");
            Assert.AreEqual(1, wave.CurrentWave,
                "Should still be wave 1");
        }

        [Test]
        public void System_DoesNotRun_WhenNoWaveData()
        {
            // Arrange — only spawner, no wave data
            CreateSpawnerData();

            // Act — should not crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _waveSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip gracefully when no WaveData exists");
        }
    }
}
