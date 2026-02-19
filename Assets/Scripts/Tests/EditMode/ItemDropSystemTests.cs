using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;
using MyGame.ECS.Enemy;
using MyGame.ECS.Item;

namespace MyGame.Tests
{
    /// <summary>
    /// ItemDropSystem EditMode tests.
    /// Validates item spawning on enemy death based on drop chance and type.
    /// </summary>
    [TestFixture]
    public class ItemDropSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _itemDropSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _itemDropSystemHandle = _world.GetOrCreateSystem<ItemDropSystem>();
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
        /// Creates an item prefab singleton entity.
        /// </summary>
        private Entity CreateItemPrefabSingleton()
        {
            // Create a prefab entity for items
            var prefab = _em.CreateEntity();
            _em.AddComponent<Prefab>(prefab);
            _em.AddComponentData(prefab, LocalTransform.FromPosition(float3.zero));

            // Create the singleton with the prefab reference
            var singleton = _em.CreateEntity();
            _em.AddComponentData(singleton, new ItemPrefabRef { Prefab = prefab });

            return prefab;
        }

        /// <summary>
        /// Creates an enemy entity with DeadTag and ItemDropData.
        /// </summary>
        private Entity CreateDeadEnemyWithDrop(
            float3? pos = null,
            int dropType = ItemData.SCORE_ITEM,
            float dropChance = 1.0f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyTag>(entity);
            _em.AddComponent<DeadTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(entity, new ItemDropData
            {
                DropType = dropType,
                DropChance = dropChance
            });
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
            _itemDropSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void Item_SpawnedOnEnemyDeath_WhenChance100()
        {
            // Arrange
            CreateItemPrefabSingleton();
            CreateDeadEnemyWithDrop(dropChance: 1.0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — at least one entity with ItemTag should exist
            var query = _em.CreateEntityQuery(typeof(ItemTag));
            Assert.IsFalse(query.IsEmpty,
                "Item should be spawned when DropChance is 1.0");
        }

        [Test]
        public void Item_NotSpawned_WhenChance0()
        {
            // Arrange
            CreateItemPrefabSingleton();
            CreateDeadEnemyWithDrop(dropChance: 0.0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(ItemTag));
            Assert.IsTrue(query.IsEmpty,
                "Item should not be spawned when DropChance is 0.0");
        }

        [Test]
        public void Item_HasCorrectPosition()
        {
            // Arrange
            var spawnPos = new float3(2f, 5f, 0f);
            CreateItemPrefabSingleton();
            CreateDeadEnemyWithDrop(pos: spawnPos, dropChance: 1.0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(ItemTag), typeof(LocalTransform));
            Assert.IsFalse(query.IsEmpty, "Item should exist");

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var itemTransform = _em.GetComponentData<LocalTransform>(entities[0]);
            Assert.AreEqual(spawnPos.x, itemTransform.Position.x, 0.01f,
                "Item X position should match enemy death position");
            Assert.AreEqual(spawnPos.y, itemTransform.Position.y, 0.01f,
                "Item Y position should match enemy death position");
            entities.Dispose();
        }

        [Test]
        public void Item_HasCorrectType()
        {
            // Arrange
            CreateItemPrefabSingleton();
            CreateDeadEnemyWithDrop(dropType: ItemData.POWER_ITEM, dropChance: 1.0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(ItemTag), typeof(ItemData));
            Assert.IsFalse(query.IsEmpty, "Item should exist");

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var itemData = _em.GetComponentData<ItemData>(entities[0]);
            Assert.AreEqual(ItemData.POWER_ITEM, itemData.Type,
                "Item type should match the enemy's DropType");
            entities.Dispose();
        }

        [Test]
        public void System_DoesNotRun_WhenNoDeadTag()
        {
            // Arrange — enemy alive (no DeadTag), but has ItemDropData
            CreateItemPrefabSingleton();
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            _em.AddComponentData(entity, new ItemDropData
            {
                DropType = ItemData.SCORE_ITEM,
                DropChance = 1.0f
            });

            // Act
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _itemDropSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            var query = _em.CreateEntityQuery(typeof(ItemTag));
            Assert.IsTrue(query.IsEmpty,
                "System should not spawn items when no DeadTag entities exist");
        }

        [Test]
        public void System_DoesNotRun_WhenNoItemPrefabRef()
        {
            // Arrange — dead enemy exists, but no ItemPrefabRef singleton
            CreateDeadEnemyWithDrop(dropChance: 1.0f);

            // Act — should not crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _itemDropSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no ItemPrefabRef singleton exists");
        }
    }
}
