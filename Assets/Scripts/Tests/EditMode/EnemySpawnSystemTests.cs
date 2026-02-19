using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;

namespace MyGame.Tests
{
    /// <summary>
    /// EnemySpawnSystem 的 EditMode 測試。
    /// 驗證敵人生成計時器、生成位置、EnemyTag 掛載。
    /// </summary>
    [TestFixture]
    public class EnemySpawnSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _spawnSystemHandle;
        private SystemHandle _ecbSystemHandle;

        /// <summary>測試用固定 DeltaTime（1/60 秒）。</summary>
        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            _spawnSystemHandle = _world.GetOrCreateSystem<EnemySpawnSystem>();
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
        /// 建立 Enemy Prefab entity（模擬真實的 EnemyAuthoring baked prefab）。
        /// </summary>
        private Entity CreateEnemyPrefabEntity()
        {
            var prefab = _em.CreateEntity();
            _em.AddComponentData(prefab, new EnemyTag());
            _em.AddComponentData(prefab, LocalTransform.FromPosition(float3.zero));
            _em.AddComponentData(prefab, new EnemyVelocity { Value = new float3(0f, -3f, 0f) });
            _em.AddComponent<Prefab>(prefab);
            return prefab;
        }

        /// <summary>
        /// 建立 EnemySpawnerData singleton。
        /// </summary>
        private Entity CreateSpawner(
            float timer = 0f,
            float interval = 2f,
            float spawnMinX = -1.5f,
            float spawnMaxX = 1.5f,
            float spawnY = 4f,
            Entity? prefab = null)
        {
            var prefabEntity = prefab ?? CreateEnemyPrefabEntity();

            var spawner = _em.CreateEntity();
            _em.AddComponentData(spawner, new EnemySpawnerData
            {
                Prefab = prefabEntity,
                Timer = timer,
                Interval = interval,
                SpawnMinX = spawnMinX,
                SpawnMaxX = spawnMaxX,
                SpawnY = spawnY
            });
            return spawner;
        }

        /// <summary>
        /// 推進時間並更新系統。
        /// </summary>
        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _spawnSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        /// <summary>
        /// 查詢場上所有非 Prefab 的 EnemyTag entity 數量。
        /// </summary>
        private int CountActiveEnemies()
        {
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Exclude<Prefab>());
            return query.CalculateEntityCount();
        }

        [Test]
        public void SpawnerCreatesEnemy_WhenTimerExpires()
        {
            // Arrange — timer 已歸零
            CreateSpawner(timer: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(1, CountActiveEnemies(),
                "One enemy should be spawned when timer expires");
        }

        [Test]
        public void SpawnerDoesNotCreate_WhenTimerRemaining()
        {
            // Arrange — timer 還有很長時間
            CreateSpawner(timer: 5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(0, CountActiveEnemies(),
                "No enemies should be spawned while timer is active");
        }

        [Test]
        public void SpawnerTimer_ResetsAfterSpawn()
        {
            // Arrange
            var interval = 2.0f;
            var spawner = CreateSpawner(timer: 0f, interval: interval);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — timer 應被重置為 interval
            var data = _em.GetComponentData<EnemySpawnerData>(spawner);
            Assert.Greater(data.Timer, 0f, "Timer should be reset after spawn");
            Assert.AreEqual(interval, data.Timer, TEST_DELTA_TIME + 0.001f,
                "Timer should be approximately equal to Interval after reset");
        }

        [Test]
        public void SpawnedEnemy_HasEnemyTag()
        {
            // Arrange
            CreateSpawner(timer: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 生成的敵人應有 EnemyTag
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Exclude<Prefab>());
            Assert.AreEqual(1, query.CalculateEntityCount(),
                "Spawned enemy should have EnemyTag");
        }

        [Test]
        public void SpawnedEnemy_PositionY_MatchesSpawnerSpawnY()
        {
            // Arrange
            var spawnY = 4.5f;
            CreateSpawner(timer: 0f, spawnY: spawnY);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<Prefab>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            Assert.AreEqual(1, entities.Length);

            var pos = _em.GetComponentData<LocalTransform>(entities[0]).Position;
            Assert.AreEqual(spawnY, pos.y, 0.001f,
                "Spawned enemy Y should match SpawnY");
            Assert.AreEqual(0f, pos.z, 0.001f,
                "Spawned enemy Z should be 0");
            entities.Dispose();
        }

        [Test]
        public void SpawnedEnemy_PositionX_WithinSpawnRange()
        {
            // Arrange — 較大的 X 範圍以測試隨機值
            var spawnMinX = -2f;
            var spawnMaxX = 2f;
            CreateSpawner(timer: 0f, spawnMinX: spawnMinX, spawnMaxX: spawnMaxX);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<Prefab>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            Assert.AreEqual(1, entities.Length);

            var pos = _em.GetComponentData<LocalTransform>(entities[0]).Position;
            Assert.GreaterOrEqual(pos.x, spawnMinX,
                "Spawned enemy X should be >= SpawnMinX");
            Assert.LessOrEqual(pos.x, spawnMaxX,
                "Spawned enemy X should be <= SpawnMaxX");
            entities.Dispose();
        }

        [Test]
        public void EnemySpawnSystem_DoesNotRun_WhenNoSpawnerSingleton()
        {
            // Arrange — 不建立 Spawner singleton

            // Act — 不應 crash（RequireForUpdate 會讓系統 skip）
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _spawnSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert — 正常結束即通過
            Assert.AreEqual(0, CountActiveEnemies(),
                "No enemies should exist when no spawner singleton");
        }
    }
}
