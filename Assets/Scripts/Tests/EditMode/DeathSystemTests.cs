using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;

namespace MyGame.Tests
{
    /// <summary>
    /// DeathSystem 的 EditMode 測試。
    /// 驗證 DeadTag Entity 銷毀、無 Tag 存活。
    /// </summary>
    [TestFixture]
    public class DeathSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _deathSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _deathSystemHandle = _world.GetOrCreateSystem<DeathSystem>();
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
        /// 推進時間並更新系統。
        /// </summary>
        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _deathSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void EntityWithDeadTag_IsDestroyed()
        {
            // Arrange
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            _em.AddComponent<DeadTag>(entity);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(entity),
                "Entity with DeadTag should be destroyed");
        }

        [Test]
        public void EntityWithoutDeadTag_Survives()
        {
            // Arrange — 沒有 DeadTag 的 entity
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));

            // 建立一個有 DeadTag 的 entity（讓系統能執行）
            var deadEntity = _em.CreateEntity();
            _em.AddComponent<DeadTag>(deadEntity);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.Exists(entity),
                "Entity without DeadTag should survive");
            Assert.IsFalse(_em.Exists(deadEntity),
                "Entity with DeadTag should be destroyed");
        }

        [Test]
        public void MultipleDeadEntities_AllDestroyed()
        {
            // Arrange — 多個 DeadTag entity
            var e1 = _em.CreateEntity();
            _em.AddComponent<DeadTag>(e1);
            var e2 = _em.CreateEntity();
            _em.AddComponent<DeadTag>(e2);
            var e3 = _em.CreateEntity();
            _em.AddComponent<DeadTag>(e3);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(e1), "Dead entity 1 should be destroyed");
            Assert.IsFalse(_em.Exists(e2), "Dead entity 2 should be destroyed");
            Assert.IsFalse(_em.Exists(e3), "Dead entity 3 should be destroyed");
        }

        [Test]
        public void System_DoesNotRun_WhenNoDeadTag()
        {
            // Arrange — 不建立任何有 DeadTag 的 entity
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));

            // Act — 不應 crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _deathSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsTrue(_em.Exists(entity),
                "Entity should survive when no DeadTag entities exist");
        }
    }
}
