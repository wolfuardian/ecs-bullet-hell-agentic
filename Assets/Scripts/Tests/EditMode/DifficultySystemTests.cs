using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using MyGame.ECS.Difficulty;
using MyGame.ECS.Enemy;

namespace MyGame.Tests
{
    /// <summary>
    /// DifficultySystem EditMode tests.
    /// Verifies elapsed time tracking, multiplier scaling, capping, and spawn interval adjustment.
    /// </summary>
    [TestFixture]
    public class DifficultySystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _difficultySystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _difficultySystemHandle = _world.GetOrCreateSystem<DifficultySystem>();
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
        /// Creates a DifficultyData singleton entity with given parameters.
        /// </summary>
        private Entity CreateDifficultySingleton(
            float elapsedTime = 0f,
            float spawnRateMultiplier = 1f,
            float maxMultiplier = 3f,
            float scalingInterval = 30f,
            float baseSpawnInterval = 2f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new DifficultyData
            {
                ElapsedTime = elapsedTime,
                SpawnRateMultiplier = spawnRateMultiplier,
                MaxMultiplier = maxMultiplier,
                ScalingInterval = scalingInterval,
                BaseSpawnInterval = baseSpawnInterval
            });
            return entity;
        }

        /// <summary>
        /// Creates an EnemySpawnerData singleton entity with given interval.
        /// </summary>
        private Entity CreateSpawnerSingleton(float interval = 2f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new EnemySpawnerData
            {
                Prefab = Entity.Null,
                Timer = 0f,
                Interval = interval,
                SpawnMinX = -1.5f,
                SpawnMaxX = 1.5f,
                SpawnY = 4f
            });
            return entity;
        }

        /// <summary>
        /// Advances time and updates the DifficultySystem.
        /// </summary>
        private void AdvanceTimeAndUpdate(float dt = 0f)
        {
            if (dt == 0f)
                dt = TEST_DELTA_TIME;

            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + dt,
                deltaTime: dt));
            _difficultySystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void ElapsedTime_IncreasesEachFrame()
        {
            // Arrange
            var diffEntity = CreateDifficultySingleton();
            CreateSpawnerSingleton();

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var data = _em.GetComponentData<DifficultyData>(diffEntity);
            Assert.AreEqual(TEST_DELTA_TIME, data.ElapsedTime, 0.0001f,
                "ElapsedTime should increase by DeltaTime each frame");
        }

        [Test]
        public void SpawnRateMultiplier_IncreasesOverTime()
        {
            // Arrange — simulate 30s of elapsed time with default scalingInterval=30
            var diffEntity = CreateDifficultySingleton(elapsedTime: 0f);
            CreateSpawnerSingleton();

            // Act — advance 30 seconds in one step
            AdvanceTimeAndUpdate(dt: 30f);

            // Assert — after 30s with scalingInterval=30, multiplier = 1 + (30/30) = 2.0
            var data = _em.GetComponentData<DifficultyData>(diffEntity);
            Assert.AreEqual(2.0f, data.SpawnRateMultiplier, 0.01f,
                "SpawnRateMultiplier should be ~2.0 after 30s at default scaling");
        }

        [Test]
        public void SpawnRateMultiplier_CapsAtMaximum()
        {
            // Arrange — maxMultiplier = 3.0, enough time to exceed it
            var diffEntity = CreateDifficultySingleton(maxMultiplier: 3f, scalingInterval: 30f);
            CreateSpawnerSingleton();

            // Act — advance 600 seconds (would give 1 + 600/30 = 21x without cap)
            AdvanceTimeAndUpdate(dt: 600f);

            // Assert
            var data = _em.GetComponentData<DifficultyData>(diffEntity);
            Assert.AreEqual(3.0f, data.SpawnRateMultiplier, 0.01f,
                "SpawnRateMultiplier should not exceed MaxMultiplier");
        }

        [Test]
        public void SpawnInterval_DecreasesAsMultiplierIncreases()
        {
            // Arrange
            var diffEntity = CreateDifficultySingleton(
                baseSpawnInterval: 2f, scalingInterval: 30f, maxMultiplier: 5f);
            var spawnerEntity = CreateSpawnerSingleton(interval: 2f);

            // Act — advance 30s => multiplier = 2.0 => interval = 2.0 / 2.0 = 1.0
            AdvanceTimeAndUpdate(dt: 30f);

            // Assert
            var spawner = _em.GetComponentData<EnemySpawnerData>(spawnerEntity);
            Assert.AreEqual(1.0f, spawner.Interval, 0.01f,
                "EnemySpawnerData.Interval should decrease as multiplier increases");
        }

        [Test]
        public void InitialState_MultiplierIsOne()
        {
            // Arrange
            var diffEntity = CreateDifficultySingleton();
            CreateSpawnerSingleton();

            // Act — advance minimal time (nearly zero effect)
            _world.SetTime(new TimeData(elapsedTime: 0.0, deltaTime: 0f));
            _difficultySystemHandle.Update(_world.Unmanaged);

            // Assert — with 0 deltaTime, elapsedTime stays 0 => multiplier = min(1+0/30, 3) = 1
            var data = _em.GetComponentData<DifficultyData>(diffEntity);
            Assert.AreEqual(1.0f, data.SpawnRateMultiplier, 0.001f,
                "At time 0, SpawnRateMultiplier should be 1.0");
        }

        [Test]
        public void System_DoesNotRun_WhenNoDifficultyData()
        {
            // Arrange — only EnemySpawnerData, no DifficultyData
            CreateSpawnerSingleton();

            // Act — should not crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _difficultySystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no DifficultyData exists");
        }

        [Test]
        public void System_DoesNotRun_WhenNoEnemySpawnerData()
        {
            // Arrange — only DifficultyData, no EnemySpawnerData
            var diffEntity = CreateDifficultySingleton();

            // Act — should not crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _difficultySystemHandle.Update(_world.Unmanaged);

            // Assert — ElapsedTime should remain 0 (system didn't run)
            var data = _em.GetComponentData<DifficultyData>(diffEntity);
            Assert.AreEqual(0f, data.ElapsedTime, 0.0001f,
                "ElapsedTime should not change when system is skipped");
        }
    }
}
