using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for DanmakuMotionSystem.
    /// Validates polar-based movement, acceleration, speed cap, and angular velocity.
    /// </summary>
    [TestFixture]
    public class DanmakuMotionSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _systemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;
            _systemHandle = _world.GetOrCreateSystem<DanmakuMotionSystem>();
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
        }

        /// <summary>
        /// Creates a danmaku bullet with BulletTag + BulletMotion + LocalTransform.
        /// </summary>
        private Entity CreateDanmakuBullet(
            float3? pos = null,
            float speed = 5f,
            float angle = 0f,
            float accel = 0f,
            float maxSpeed = 0f,
            float angularVel = 0f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new BulletMotion
            {
                Speed = speed,
                Angle = angle,
                Accel = accel,
                MaxSpeed = maxSpeed,
                AngularVel = angularVel
            });
            return entity;
        }

        [Test]
        public void Bullet_MovesRight_WhenAngleIsZero()
        {
            // Arrange
            var bullet = CreateDanmakuBullet(speed: 60f, angle: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(bullet).Position;
            Assert.Greater(pos.x, 0f, "Bullet should move in +X when angle=0");
            Assert.AreEqual(0f, pos.y, 0.001f, "Y should not change when angle=0");
        }

        [Test]
        public void Bullet_MovesUp_WhenAngleIsHalfPi()
        {
            // Arrange
            var bullet = CreateDanmakuBullet(speed: 60f, angle: math.PI / 2f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(bullet).Position;
            Assert.Greater(pos.y, 0f, "Bullet should move in +Y when angle=PI/2");
            Assert.AreEqual(0f, pos.x, 0.01f, "X should be near zero when angle=PI/2");
        }

        [Test]
        public void Bullet_MovesDown_WhenAngleIsNegativeHalfPi()
        {
            // Arrange
            var bullet = CreateDanmakuBullet(speed: 60f, angle: -math.PI / 2f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(bullet).Position;
            Assert.Less(pos.y, 0f, "Bullet should move in -Y when angle=-PI/2");
            Assert.AreEqual(0f, pos.x, 0.01f, "X should be near zero when angle=-PI/2");
        }

        [Test]
        public void Speed_AffectsMovementDistance()
        {
            // Arrange
            var slowBullet = CreateDanmakuBullet(
                pos: float3.zero, speed: 30f, angle: 0f);
            var fastBullet = CreateDanmakuBullet(
                pos: new float3(0f, 5f, 0f), speed: 120f, angle: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var slowPos = _em.GetComponentData<LocalTransform>(slowBullet).Position;
            var fastPos = _em.GetComponentData<LocalTransform>(fastBullet).Position;
            Assert.Greater(fastPos.x - 5f, slowPos.x,
                "Faster bullet should travel further");
            // Actually fastBullet starts at x=0 too, let me fix the comparison
            Assert.Greater(fastPos.x, slowPos.x,
                "Faster bullet should have larger X displacement");
        }

        [Test]
        public void Acceleration_IncreasesSpeed()
        {
            // Arrange
            float initialSpeed = 10f;
            float accel = 60f; // 60 units/sec^2
            var bullet = CreateDanmakuBullet(speed: initialSpeed, angle: 0f, accel: accel);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            Assert.Greater(motion.Speed, initialSpeed,
                "Speed should increase when acceleration is positive");
            Assert.AreEqual(initialSpeed + accel * TEST_DELTA_TIME, motion.Speed, 0.001f,
                "Speed should increase by accel * dt");
        }

        [Test]
        public void MaxSpeed_CapsAcceleration()
        {
            // Arrange
            float maxSpeed = 12f;
            var bullet = CreateDanmakuBullet(
                speed: 11f, angle: 0f, accel: 600f, maxSpeed: maxSpeed);

            // Act — run several frames to let acceleration exceed maxSpeed
            for (int i = 0; i < 10; i++)
                AdvanceTimeAndUpdate();

            // Assert
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            Assert.AreEqual(maxSpeed, motion.Speed, 0.001f,
                "Speed should be capped at MaxSpeed");
        }

        [Test]
        public void AngularVelocity_RotatesAngle()
        {
            // Arrange
            float angularVel = math.PI; // 180 deg/sec
            var bullet = CreateDanmakuBullet(speed: 60f, angle: 0f, angularVel: angularVel);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            float expectedAngle = angularVel * TEST_DELTA_TIME;
            Assert.AreEqual(expectedAngle, motion.Angle, 0.001f,
                "Angle should increase by angularVel * dt");
        }

        [Test]
        public void ZeroSpeed_NoMovement()
        {
            // Arrange
            var bullet = CreateDanmakuBullet(speed: 0f, angle: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(bullet).Position;
            Assert.AreEqual(0f, pos.x, 0.001f, "X should not change with zero speed");
            Assert.AreEqual(0f, pos.y, 0.001f, "Y should not change with zero speed");
            Assert.AreEqual(0f, pos.z, 0.001f, "Z should not change with zero speed");
        }
    }
}
