using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;
using MyGame.ECS.Player;
using MyGame.ECS.Score;
using MyGame.ECS.Bomb;
using MyGame.ECS.Item;

namespace MyGame.Tests
{
    /// <summary>
    /// ItemCollectionSystem EditMode tests.
    /// Validates score, power, and bomb item collection, destruction,
    /// out-of-range handling, and power level capping.
    /// </summary>
    [TestFixture]
    public class ItemCollectionSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _collectionSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _collectionSystemHandle = _world.GetOrCreateSystem<ItemCollectionSystem>();
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
        /// Creates a player entity with standard components.
        /// </summary>
        private Entity CreatePlayer(
            float3? pos = null,
            float radius = 0.08f,
            int powerLevel = 0,
            int maxPower = 4,
            int bombStock = 3)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlayerTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new HealthData { Current = 1, Max = 1 });
            _em.AddComponentData(entity, new InvincibilityTimer { Value = 0f });
            _em.AddComponentData(entity, new InvincibilityDuration { Value = 2f });
            _em.AddComponentData(entity, new PowerLevelData
            {
                Level = powerLevel,
                MaxLevel = maxPower
            });
            _em.AddComponentData(entity, new BombData
            {
                Stock = bombStock,
                CooldownTimer = 0f,
                CooldownDuration = 1f,
                BombDuration = 3f
            });
            return entity;
        }

        /// <summary>
        /// Creates an item entity at the specified position.
        /// </summary>
        private Entity CreateItem(
            float3? pos = null,
            float radius = 0.2f,
            int type = ItemData.SCORE_ITEM,
            int scoreValue = 100,
            int powerValue = 1)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<ItemTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new ItemData
            {
                Type = type,
                ScoreValue = scoreValue,
                PowerValue = powerValue
            });
            _em.AddComponentData(entity, new ItemVelocity
            {
                Value = new float3(0f, -2f, 0f)
            });
            _em.AddComponentData(entity, new ItemLifetime { Value = 10f });
            return entity;
        }

        /// <summary>
        /// Creates the ScoreData singleton.
        /// </summary>
        private Entity CreateScoreSingleton(int initialScore = 0)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new ScoreData { Value = initialScore });
            return entity;
        }

        /// <summary>
        /// Advances time and updates the system + ECB.
        /// </summary>
        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collectionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void ScoreItem_IncreasesScore()
        {
            // Arrange — player and score item at same position
            CreatePlayer(pos: new float3(0f, 0f, 0f));
            CreateScoreSingleton(initialScore: 0);
            CreateItem(pos: new float3(0f, 0f, 0f), type: ItemData.SCORE_ITEM, scoreValue: 200);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var scoreQuery = _em.CreateEntityQuery(typeof(ScoreData));
            var score = scoreQuery.GetSingleton<ScoreData>();
            Assert.AreEqual(200, score.Value,
                "Score should increase by item's ScoreValue");
        }

        [Test]
        public void PowerItem_IncreasesPowerLevel()
        {
            // Arrange
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), powerLevel: 1, maxPower: 4);
            CreateScoreSingleton();
            CreateItem(pos: new float3(0f, 0f, 0f), type: ItemData.POWER_ITEM, powerValue: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var power = _em.GetComponentData<PowerLevelData>(player);
            Assert.AreEqual(2, power.Level,
                "Power level should increase by item's PowerValue");
        }

        [Test]
        public void BombItem_IncreasesBombStock()
        {
            // Arrange
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), bombStock: 2);
            CreateScoreSingleton();
            CreateItem(pos: new float3(0f, 0f, 0f), type: ItemData.BOMB_ITEM);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var bomb = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(3, bomb.Stock,
                "Bomb stock should increase by 1");
        }

        [Test]
        public void Item_DestroyedAfterCollection()
        {
            // Arrange
            CreatePlayer(pos: new float3(0f, 0f, 0f));
            CreateScoreSingleton();
            var item = CreateItem(pos: new float3(0f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(item),
                "Item should be destroyed after collection");
        }

        [Test]
        public void Item_NotCollected_WhenOutOfRange()
        {
            // Arrange — player and item far apart
            CreatePlayer(pos: new float3(-10f, 0f, 0f));
            CreateScoreSingleton(initialScore: 0);
            var item = CreateItem(pos: new float3(10f, 0f, 0f), type: ItemData.SCORE_ITEM, scoreValue: 100);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.Exists(item),
                "Item should survive when out of range");
            var scoreQuery = _em.CreateEntityQuery(typeof(ScoreData));
            var score = scoreQuery.GetSingleton<ScoreData>();
            Assert.AreEqual(0, score.Value,
                "Score should remain unchanged when item is not collected");
        }

        [Test]
        public void PowerLevel_CapsAtMaxLevel()
        {
            // Arrange — power already at max-1, item gives +2
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), powerLevel: 3, maxPower: 4);
            CreateScoreSingleton();
            CreateItem(pos: new float3(0f, 0f, 0f), type: ItemData.POWER_ITEM, powerValue: 2);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var power = _em.GetComponentData<PowerLevelData>(player);
            Assert.AreEqual(4, power.Level,
                "Power level should be capped at MaxLevel");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPlayer()
        {
            // Arrange — items exist, but no player
            CreateItem(pos: new float3(0f, 0f, 0f));

            // Act — should not crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collectionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no PlayerTag entities exist");
        }
    }
}
