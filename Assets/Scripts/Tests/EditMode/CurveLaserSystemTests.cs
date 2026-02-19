using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for CurveLaserSystem.
    /// Validates point insertion, buffer trimming, duration expiry, and head movement.
    /// </summary>
    [TestFixture]
    public class CurveLaserSystemTests
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
            _systemHandle = _world.GetOrCreateSystem<CurveLaserSystem>();
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
        /// Creates a curve laser entity directly (without ECB).
        /// </summary>
        private Entity CreateCurveLaser(
            float3? origin = null,
            float speed = 5f,
            float angle = 0f,
            float width = 0.3f,
            int segmentCount = 10,
            float duration = 5f,
            float accel = 0f,
            float angularVel = 0f)
        {
            var pos = origin ?? float3.zero;
            var entity = _em.CreateEntity();
            _em.AddComponent<LaserTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos));
            _em.AddComponentData(entity, new CurveLaser
            {
                Width = width,
                Color = BulletColor.Blue,
                SegmentCount = segmentCount,
                Duration = duration,
            });
            _em.AddComponentData(entity, new BulletMotion
            {
                Speed = speed,
                Angle = angle,
                Accel = accel,
                MaxSpeed = 0f,
                AngularVel = angularVel,
            });

            var buffer = _em.AddBuffer<CurveLaserPoint>(entity);
            buffer.Add(new CurveLaserPoint
            {
                Position = pos,
                Velocity = new float3(math.cos(angle), math.sin(angle), 0f) * speed,
            });

            return entity;
        }

        [Test]
        public void PointsAddedEachFrame()
        {
            // Arrange — starts with 1 point, segment count allows growth
            var laser = CreateCurveLaser(segmentCount: 20);

            // Act — run 5 frames
            for (int i = 0; i < 5; i++)
                AdvanceTimeAndUpdate();

            // Assert — should have 6 points (1 initial + 5 added)
            var buffer = _em.GetBuffer<CurveLaserPoint>(laser);
            Assert.AreEqual(6, buffer.Length,
                "Buffer should have 1 initial + 5 added points after 5 frames");
        }

        [Test]
        public void BufferTrimmedToSegmentCount()
        {
            // Arrange — segmentCount of 4, run more frames than that
            var laser = CreateCurveLaser(segmentCount: 4);

            // Act — run 10 frames (would add 10 points without trimming)
            for (int i = 0; i < 10; i++)
                AdvanceTimeAndUpdate();

            // Assert — buffer should be trimmed to SegmentCount
            var buffer = _em.GetBuffer<CurveLaserPoint>(laser);
            Assert.AreEqual(4, buffer.Length,
                "Buffer should be trimmed to SegmentCount");
        }

        [Test]
        public void Duration_ExpiresDestroysEntity()
        {
            // Arrange — very short duration
            var laser = CreateCurveLaser(duration: TEST_DELTA_TIME * 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(laser),
                "Curve laser entity should be destroyed when duration expires");
        }

        [Test]
        public void HeadPositionUpdates()
        {
            // Arrange — moving right (angle=0) at speed 60
            var laser = CreateCurveLaser(
                origin: float3.zero, speed: 60f, angle: 0f, segmentCount: 20);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — head entity should have moved in +X
            var transform = _em.GetComponentData<LocalTransform>(laser);
            Assert.Greater(transform.Position.x, 0f,
                "Head entity should move in +X direction");
            Assert.AreEqual(60f * TEST_DELTA_TIME, transform.Position.x, 0.01f,
                "Head should move by speed * dt");
        }
    }
}
