using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Danmaku;
using MyGame.ECS.Player;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for HomingSystem.
    /// Validates homing rotation toward the player using shortest path.
    /// </summary>
    [TestFixture]
    public class HomingSystemTests
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
            _systemHandle = _world.GetOrCreateSystem<HomingSystem>();
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
        /// Creates a player entity at the given position.
        /// </summary>
        private Entity CreatePlayer(float3? pos = null)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlayerTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(
                pos ?? new float3(0f, -4f, 0f)));
            return entity;
        }

        /// <summary>
        /// Creates a homing bullet with HomingTag + BulletTag + BulletMotion.
        /// </summary>
        private Entity CreateHomingBullet(
            float3? pos = null,
            float speed = 5f,
            float angle = 0f,
            float angularVel = math.PI)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponent<HomingTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new BulletMotion
            {
                Speed = speed,
                Angle = angle,
                Accel = 0f,
                MaxSpeed = 0f,
                AngularVel = angularVel
            });
            return entity;
        }

        /// <summary>
        /// Creates a non-homing bullet (no HomingTag).
        /// </summary>
        private Entity CreateNonHomingBullet(
            float3? pos = null,
            float angle = 0f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new BulletMotion
            {
                Speed = 5f,
                Angle = angle,
                Accel = 0f,
                MaxSpeed = 0f,
                AngularVel = math.PI
            });
            return entity;
        }

        [Test]
        public void HomingBullet_RotatesTowardPlayer()
        {
            // Arrange — bullet at origin facing right (angle=0), player above
            CreatePlayer(pos: new float3(0f, 5f, 0f));
            var bullet = CreateHomingBullet(
                pos: float3.zero,
                angle: 0f,
                angularVel: math.PI * 10f); // fast rotation

            // Act
            AdvanceTimeAndUpdate();

            // Assert — angle should move toward PI/2 (up toward player)
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            Assert.Greater(motion.Angle, 0f,
                "Angle should increase toward player above (PI/2)");
        }

        [Test]
        public void HomingBullet_RotationRespectsAngularVelocity()
        {
            // Arrange — bullet facing right, player directly above
            // Use a slow angular velocity so it won't reach target in one frame
            float angularVel = math.PI / 4f; // 45 deg/sec
            CreatePlayer(pos: new float3(0f, 10f, 0f));
            var bullet = CreateHomingBullet(
                pos: float3.zero,
                angle: 0f,
                angularVel: angularVel);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — should turn by at most angularVel * dt
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            float maxTurn = angularVel * TEST_DELTA_TIME;
            Assert.LessOrEqual(math.abs(motion.Angle), maxTurn + 0.001f,
                "Rotation should be clamped to angularVel * dt per frame");
            Assert.Greater(motion.Angle, 0f,
                "Should be rotating toward player (positive direction)");
        }

        [Test]
        public void NonHomingBullet_NotAffected()
        {
            // Arrange — bullet without HomingTag
            CreatePlayer(pos: new float3(0f, 10f, 0f));
            float initialAngle = 0f;
            var bullet = CreateNonHomingBullet(pos: float3.zero, angle: initialAngle);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            Assert.AreEqual(initialAngle, motion.Angle, 0.001f,
                "Non-homing bullet angle should not change");
        }

        [Test]
        public void HomingBullet_PlayerInDifferentQuadrants()
        {
            // Test player to the left (quadrant II)
            CreatePlayer(pos: new float3(-5f, 5f, 0f));
            var bullet = CreateHomingBullet(
                pos: float3.zero,
                angle: 0f,
                angularVel: math.PI * 20f); // very fast turn

            // Act — run multiple frames to converge
            for (int i = 0; i < 60; i++)
                AdvanceTimeAndUpdate();

            // Assert — angle should converge toward atan2(5, -5) = 3*PI/4
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            float targetAngle = math.atan2(5f, -5f);
            Assert.AreEqual(targetAngle, motion.Angle, 0.1f,
                "Homing bullet should converge toward player in quadrant II");
        }

        [Test]
        public void HomingBullet_PlayerBelow()
        {
            // Arrange — bullet facing right, player below
            CreatePlayer(pos: new float3(0f, -10f, 0f));
            var bullet = CreateHomingBullet(
                pos: float3.zero,
                angle: 0f,
                angularVel: math.PI * 10f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — angle should move toward -PI/2 (down toward player)
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            Assert.Less(motion.Angle, 0f,
                "Angle should decrease toward player below (-PI/2)");
        }

        [Test]
        public void HomingBullet_PlayerBehind_TakesShortestPath()
        {
            // Arrange — bullet facing right (angle=0), player to the left
            // Shortest path to PI is either direction; test it turns
            CreatePlayer(pos: new float3(-10f, 0.1f, 0f));
            var bullet = CreateHomingBullet(
                pos: float3.zero,
                angle: 0f,
                angularVel: math.PI * 10f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — should rotate (angle should change from 0)
            var motion = _em.GetComponentData<BulletMotion>(bullet);
            Assert.AreNotEqual(0f, motion.Angle,
                "Bullet should rotate toward player behind it");
        }
    }
}
