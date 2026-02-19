using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for LaserBeamSystem.
    /// Validates warning phase, growth, max length cap, duration expiry, and inactive behaviour.
    /// </summary>
    [TestFixture]
    public class LaserBeamSystemTests
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
            _systemHandle = _world.GetOrCreateSystem<LaserBeamSystem>();
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
        /// Creates a beam laser entity directly (without ECB).
        /// </summary>
        private Entity CreateBeamLaser(
            float3? origin = null,
            float angle = 0f,
            float maxLength = 10f,
            float width = 0.5f,
            float growSpeed = 20f,
            float warningTime = 0.5f,
            float duration = 3f,
            bool active = false,
            float length = 0f)
        {
            var pos = origin ?? float3.zero;
            var entity = _em.CreateEntity();
            _em.AddComponent<LaserTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos));
            _em.AddComponentData(entity, new LaserBeam
            {
                Origin = pos,
                Angle = angle,
                Length = length,
                MaxLength = maxLength,
                Width = width,
                GrowSpeed = growSpeed,
                Color = BulletColor.Red,
                Active = active,
                WarningTimer = warningTime,
                Duration = duration,
            });
            return entity;
        }

        [Test]
        public void WarningPhase_DoesNotActivate()
        {
            // Arrange — warning time is 1 second, only run 1 frame (1/60s)
            var laser = CreateBeamLaser(warningTime: 1f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var beam = _em.GetComponentData<LaserBeam>(laser);
            Assert.IsFalse(beam.Active,
                "Beam should remain inactive during warning phase");
        }

        [Test]
        public void WarningPhase_Expires_ActivatesBeam()
        {
            // Arrange — very short warning time, 1 frame should expire it
            var laser = CreateBeamLaser(warningTime: TEST_DELTA_TIME * 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var beam = _em.GetComponentData<LaserBeam>(laser);
            Assert.IsTrue(beam.Active,
                "Beam should become active after warning timer expires");
        }

        [Test]
        public void GrowthPhase_IncreasesLength()
        {
            // Arrange — skip warning phase by starting active
            var laser = CreateBeamLaser(active: true, warningTime: 0f, growSpeed: 60f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var beam = _em.GetComponentData<LaserBeam>(laser);
            Assert.Greater(beam.Length, 0f,
                "Beam length should increase during growth phase");
            Assert.AreEqual(60f * TEST_DELTA_TIME, beam.Length, 0.001f,
                "Length should increase by GrowSpeed * dt");
        }

        [Test]
        public void GrowthPhase_CapsAtMaxLength()
        {
            // Arrange — growSpeed is very high, should cap at MaxLength
            var laser = CreateBeamLaser(
                active: true, warningTime: 0f,
                growSpeed: 10000f, maxLength: 5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var beam = _em.GetComponentData<LaserBeam>(laser);
            Assert.AreEqual(5f, beam.Length, 0.001f,
                "Beam length should be capped at MaxLength");
        }

        [Test]
        public void Duration_ExpiresDestroysEntity()
        {
            // Arrange — active with very short duration
            var laser = CreateBeamLaser(
                active: true, warningTime: 0f,
                duration: TEST_DELTA_TIME * 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(laser),
                "Beam entity should be destroyed when duration expires");
        }

        [Test]
        public void InactiveBeam_DoesNotGrow()
        {
            // Arrange — beam is in warning phase
            var laser = CreateBeamLaser(warningTime: 10f, growSpeed: 100f);

            // Act — run several frames
            for (int i = 0; i < 10; i++)
                AdvanceTimeAndUpdate();

            // Assert
            var beam = _em.GetComponentData<LaserBeam>(laser);
            Assert.AreEqual(0f, beam.Length, 0.001f,
                "Beam length should stay 0 during warning phase");
        }
    }
}
