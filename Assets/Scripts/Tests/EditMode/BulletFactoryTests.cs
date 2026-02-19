using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for BulletFactory.
    /// Validates that factory methods create entities with correct components.
    /// </summary>
    [TestFixture]
    public class BulletFactoryTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _ecbSystemHandle;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;
            _ecbSystemHandle = _world.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
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
        /// Creates an ECB, runs the action, then plays back and returns bullet count.
        /// </summary>
        private EntityCommandBuffer CreateECB()
        {
            var ecbSingleton = _world.EntityManager.CreateEntity();
            // Manually create an ECB for testing
            return new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
        }

        private void PlaybackAndDispose(EntityCommandBuffer ecb)
        {
            ecb.Playback(_em);
            ecb.Dispose();
        }

        private int CountBullets()
        {
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.ReadOnly<EnemyBulletTag>());
            int count = query.CalculateEntityCount();
            query.Dispose();
            return count;
        }

        [Test]
        public void Shot_CreatesEntityWithAllRequiredComponents()
        {
            // Arrange
            var ecb = CreateECB();
            var pos = new float3(1f, 2f, 0f);
            float speed = 8f;
            float angle = -math.PI * 0.5f;

            // Act
            BulletFactory.Shot(ref ecb, pos, speed, angle,
                BulletShape.BallS, BulletColor.Red);
            PlaybackAndDispose(ecb);

            // Assert
            Assert.AreEqual(1, CountBullets(), "Should create exactly 1 bullet");

            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletTag>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var entity = entities[0];
                Assert.IsTrue(_em.HasComponent<BulletTag>(entity));
                Assert.IsTrue(_em.HasComponent<EnemyBulletTag>(entity));
                Assert.IsTrue(_em.HasComponent<BulletVisual>(entity));
                Assert.IsTrue(_em.HasComponent<BulletMotion>(entity));
                Assert.IsTrue(_em.HasComponent<BulletHitbox>(entity));
                Assert.IsTrue(_em.HasComponent<BulletLifetime>(entity));
                Assert.IsTrue(_em.HasComponent<DamageOnContact>(entity));
                Assert.IsTrue(_em.HasComponent<BulletFlags>(entity));
                Assert.IsTrue(_em.HasComponent<LocalTransform>(entity));
            }
            query.Dispose();
        }

        [Test]
        public void Shot_SetsCorrectAngle()
        {
            // Arrange
            var ecb = CreateECB();
            float angle = 1.234f;

            // Act
            BulletFactory.Shot(ref ecb, float3.zero, 5f, angle,
                BulletShape.Pellet, BulletColor.White);
            PlaybackAndDispose(ecb);

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var motion = _em.GetComponentData<BulletMotion>(entities[0]);
                Assert.AreEqual(angle, motion.Angle, 0.001f,
                    "BulletMotion.Angle should match the input angle");
            }
            query.Dispose();
        }

        [Test]
        public void Shot_SetsCorrectVisual()
        {
            // Arrange
            var ecb = CreateECB();

            // Act
            BulletFactory.Shot(ref ecb, float3.zero, 5f, 0f,
                BulletShape.Kunai, BulletColor.Blue);
            PlaybackAndDispose(ecb);

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletVisual>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var visual = _em.GetComponentData<BulletVisual>(entities[0]);
                Assert.AreEqual(BulletShape.Kunai, visual.Shape);
                Assert.AreEqual(BulletColor.Blue, visual.Color);
            }
            query.Dispose();
        }

        [Test]
        public void ShotFan_CreatesCorrectNumberOfBullets()
        {
            // Arrange
            var ecb = CreateECB();
            int count = 5;

            // Act
            BulletFactory.ShotFan(ref ecb, float3.zero, 8f,
                -math.PI * 0.5f, math.PI / 3f, count,
                BulletShape.BallS, BulletColor.Red);
            PlaybackAndDispose(ecb);

            // Assert
            Assert.AreEqual(count, CountBullets(),
                $"ShotFan should create {count} bullets");
        }

        [Test]
        public void ShotFan_SingleBullet_CreatesOne()
        {
            // Arrange
            var ecb = CreateECB();

            // Act
            BulletFactory.ShotFan(ref ecb, float3.zero, 8f,
                -math.PI * 0.5f, math.PI / 3f, 1,
                BulletShape.BallS, BulletColor.Red);
            PlaybackAndDispose(ecb);

            // Assert
            Assert.AreEqual(1, CountBullets(),
                "ShotFan with count=1 should create 1 bullet");
        }

        [Test]
        public void ShotRing_CreatesCorrectNumberWithEvenSpacing()
        {
            // Arrange
            var ecb = CreateECB();
            int count = 8;

            // Act
            BulletFactory.ShotRing(ref ecb, float3.zero, 5f, 0f, count,
                BulletShape.BallS, BulletColor.White);
            PlaybackAndDispose(ecb);

            // Assert
            Assert.AreEqual(count, CountBullets(),
                $"ShotRing should create {count} bullets");

            // Verify angle spacing
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                float expectedStep = math.PI * 2f / count;
                for (int i = 0; i < entities.Length - 1; i++)
                {
                    var motion0 = _em.GetComponentData<BulletMotion>(entities[i]);
                    var motion1 = _em.GetComponentData<BulletMotion>(entities[i + 1]);
                    float diff = math.abs(motion1.Angle - motion0.Angle);
                    Assert.AreEqual(expectedStep, diff, 0.01f,
                        $"Ring bullets {i} and {i + 1} should be {expectedStep} radians apart");
                }
            }
            query.Dispose();
        }

        [Test]
        public void ShotAim_CalculatesCorrectAngleTowardTarget()
        {
            // Arrange
            var ecb = CreateECB();
            var pos = new float3(0f, 5f, 0f);
            var targetPos = new float3(3f, -4f, 0f);
            float expectedAngle = math.atan2(targetPos.y - pos.y, targetPos.x - pos.x);

            // Act
            BulletFactory.ShotAim(ref ecb, pos, 8f, targetPos,
                BulletShape.BallS, BulletColor.Red);
            PlaybackAndDispose(ecb);

            // Assert
            Assert.AreEqual(1, CountBullets());

            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var motion = _em.GetComponentData<BulletMotion>(entities[0]);
                Assert.AreEqual(expectedAngle, motion.Angle, 0.001f,
                    "ShotAim should set angle toward target position");
            }
            query.Dispose();
        }

        [Test]
        public void ShotAccel_SetsAccelAndMaxSpeed()
        {
            // Arrange
            var ecb = CreateECB();
            float accel = 2.5f;
            float maxSpeed = 15f;

            // Act
            BulletFactory.ShotAccel(ref ecb, float3.zero, 5f, 0f,
                accel, maxSpeed, BulletShape.Pellet, BulletColor.White);
            PlaybackAndDispose(ecb);

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var motion = _em.GetComponentData<BulletMotion>(entities[0]);
                Assert.AreEqual(accel, motion.Accel, 0.001f,
                    "ShotAccel should set Accel on BulletMotion");
                Assert.AreEqual(maxSpeed, motion.MaxSpeed, 0.001f,
                    "ShotAccel should set MaxSpeed on BulletMotion");
            }
            query.Dispose();
        }

        [Test]
        public void ShotHoming_AddsHomingTag()
        {
            // Arrange
            var ecb = CreateECB();

            // Act
            BulletFactory.ShotHoming(ref ecb, float3.zero, 5f, 0f,
                1.5f, BulletShape.BallS, BulletColor.White);
            PlaybackAndDispose(ecb);

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<HomingTag>());
            int homingCount = query.CalculateEntityCount();
            query.Dispose();
            Assert.AreEqual(1, homingCount,
                "ShotHoming should add HomingTag to the bullet");
        }

        [Test]
        public void SpawnDelay_AddedOnlyWhenPositive()
        {
            // Arrange & Act: bullet WITH delay
            var ecb1 = CreateECB();
            BulletFactory.Shot(ref ecb1, float3.zero, 5f, 0f,
                BulletShape.Pellet, BulletColor.White, delay: 5);
            PlaybackAndDispose(ecb1);

            var queryDelay = _em.CreateEntityQuery(
                ComponentType.ReadOnly<SpawnDelay>());
            int delayCount = queryDelay.CalculateEntityCount();
            queryDelay.Dispose();
            Assert.AreEqual(1, delayCount,
                "SpawnDelay should be added when delay > 0");

            // Check delay value
            var queryAll = _em.CreateEntityQuery(
                ComponentType.ReadOnly<SpawnDelay>());
            using (var entities = queryAll.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var spawnDelay = _em.GetComponentData<SpawnDelay>(entities[0]);
                Assert.AreEqual(5, spawnDelay.FramesRemaining);
            }
            queryAll.Dispose();
        }

        [Test]
        public void SpawnDelay_NotAddedWhenZero()
        {
            // Arrange & Act: bullet WITHOUT delay
            var ecb2 = CreateECB();
            BulletFactory.Shot(ref ecb2, float3.zero, 5f, 0f,
                BulletShape.Pellet, BulletColor.White, delay: 0);
            PlaybackAndDispose(ecb2);

            var queryNoDelay = _em.CreateEntityQuery(
                ComponentType.ReadOnly<SpawnDelay>());
            int noDelayCount = queryNoDelay.CalculateEntityCount();
            queryNoDelay.Dispose();
            Assert.AreEqual(0, noDelayCount,
                "SpawnDelay should NOT be added when delay == 0");
        }

        [Test]
        public void ShotAimFan_CreatesFanAimedAtTarget()
        {
            // Arrange
            var ecb = CreateECB();
            var pos = new float3(0f, 5f, 0f);
            var targetPos = new float3(0f, -4f, 0f);
            int count = 3;

            // Act
            BulletFactory.ShotAimFan(ref ecb, pos, 8f, targetPos,
                math.PI / 3f, count,
                BulletShape.BallS, BulletColor.Red);
            PlaybackAndDispose(ecb);

            // Assert
            Assert.AreEqual(count, CountBullets(),
                "ShotAimFan should create fan count bullets");

            // Center bullet should aim roughly at target
            float expectedCenter = math.atan2(targetPos.y - pos.y, targetPos.x - pos.x);
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                // Middle bullet (index 1 of 3) should be closest to center
                var middleMotion = _em.GetComponentData<BulletMotion>(entities[1]);
                Assert.AreEqual(expectedCenter, middleMotion.Angle, 0.01f,
                    "Middle bullet of ShotAimFan should aim at target");
            }
            query.Dispose();
        }

        [Test]
        public void ShotSpread_CreatesCorrectCount()
        {
            // Arrange
            var ecb = CreateECB();
            int count = 10;

            // Act
            BulletFactory.ShotSpread(ref ecb, float3.zero, -math.PI * 0.5f,
                0.5f, new float2(3f, 10f), count,
                BulletShape.Pellet, BulletColor.White, seed: 42u);
            PlaybackAndDispose(ecb);

            // Assert
            Assert.AreEqual(count, CountBullets(),
                $"ShotSpread should create {count} bullets");
        }

        [Test]
        public void Shot_SetsHitboxFromShapeTable()
        {
            // Arrange
            var ecb = CreateECB();

            // Act
            BulletFactory.Shot(ref ecb, float3.zero, 5f, 0f,
                BulletShape.BallS, BulletColor.White);
            PlaybackAndDispose(ecb);

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletHitbox>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var hitbox = _em.GetComponentData<BulletHitbox>(entities[0]);
                var expected = BulletShapeTable.Get(BulletShape.BallS);
                Assert.AreEqual(expected.Hitbox.Type, hitbox.Type,
                    "Hitbox type should match shape table");
                Assert.AreEqual(expected.Hitbox.Size.x, hitbox.Size.x, 0.001f,
                    "Hitbox size should match shape table");
            }
            query.Dispose();
        }
    }
}
