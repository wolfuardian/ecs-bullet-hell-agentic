using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for SpawnDelaySystem.
    /// Validates frame countdown and component removal.
    /// </summary>
    [TestFixture]
    public class SpawnDelaySystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _systemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;
            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _systemHandle = _world.GetOrCreateSystem<SpawnDelaySystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _systemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        /// <summary>
        /// Creates an entity with SpawnDelay component.
        /// </summary>
        private Entity CreateDelayedEntity(int framesRemaining)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new SpawnDelay
            {
                FramesRemaining = framesRemaining
            });
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            return entity;
        }

        [Test]
        public void Delay_DecrementsEachFrame()
        {
            // Arrange
            int initialFrames = 5;
            var entity = CreateDelayedEntity(initialFrames);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var delay = _em.GetComponentData<SpawnDelay>(entity);
            Assert.AreEqual(initialFrames - 1, delay.FramesRemaining,
                "FramesRemaining should decrement by 1 each frame");
        }

        [Test]
        public void SpawnDelay_RemovedWhenReachesZero()
        {
            // Arrange — 1 frame remaining, should be removed after update
            var entity = CreateDelayedEntity(1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.HasComponent<SpawnDelay>(entity),
                "SpawnDelay component should be removed when FramesRemaining reaches 0");
        }

        [Test]
        public void Entity_StillExistsAfterDelayExpires()
        {
            // Arrange
            var entity = CreateDelayedEntity(1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.Exists(entity),
                "Entity should still exist after SpawnDelay is removed");
            Assert.IsTrue(_em.HasComponent<LocalTransform>(entity),
                "Entity should retain other components after SpawnDelay removal");
        }

        [Test]
        public void SpawnDelay_NotRemoved_WhenFramesRemaining()
        {
            // Arrange — multiple frames left
            var entity = CreateDelayedEntity(10);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.HasComponent<SpawnDelay>(entity),
                "SpawnDelay should still be present when frames remain");
            Assert.AreEqual(9, _em.GetComponentData<SpawnDelay>(entity).FramesRemaining,
                "Should have decremented to 9");
        }

        [Test]
        public void MultipleFrames_CountDownCorrectly()
        {
            // Arrange
            var entity = CreateDelayedEntity(3);

            // Act — advance 2 frames
            AdvanceTimeAndUpdate();
            AdvanceTimeAndUpdate();

            // Assert — should have 1 frame left
            Assert.IsTrue(_em.HasComponent<SpawnDelay>(entity),
                "SpawnDelay should still exist after 2 of 3 frames");
            Assert.AreEqual(1, _em.GetComponentData<SpawnDelay>(entity).FramesRemaining,
                "Should have 1 frame remaining after 2 updates");

            // Act — advance 1 more frame
            AdvanceTimeAndUpdate();

            // Assert — should be removed now
            Assert.IsFalse(_em.HasComponent<SpawnDelay>(entity),
                "SpawnDelay should be removed after 3 frames");
            Assert.IsTrue(_em.Exists(entity),
                "Entity should still exist");
        }
    }
}
