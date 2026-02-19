using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;
using MyGame.ECS.Enemy;
using MyGame.ECS.Player;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for DanmakuPatternSystem.
    /// Validates Straight, Fan, Spiral, Aimed, Ring, and Spread patterns
    /// using the new BulletFactory-based system.
    /// </summary>
    [TestFixture]
    public class DanmakuPatternSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _patternSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;
        private const float BULLET_SPEED = 8f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            _patternSystemHandle = _world.GetOrCreateSystem<DanmakuPatternSystem>();
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
        /// Creates an enemy entity with DanmakuPattern and shooting components.
        /// Cooldown is set to 0 so it fires immediately on next update.
        /// </summary>
        private Entity CreatePatternEnemy(
            DanmakuPatternType patternType,
            int bulletCount = 1,
            float spreadAngle = 1.047f,
            float spiralSpeed = 0.262f,
            float3? pos = null)
        {
            var enemy = _em.CreateEntity();

            _em.AddComponent<EnemyTag>(enemy);
            _em.AddComponentData(enemy, LocalTransform.FromPosition(
                pos ?? new float3(0f, 5f, 0f)));
            _em.AddComponentData(enemy, new EnemyVelocity
            {
                Value = new float3(0f, -3f, 0f)
            });
            _em.AddComponentData(enemy, new EnemyShootCooldown
            {
                Timer = 0f, // ready to fire immediately
                Duration = 1f
            });
            _em.AddComponentData(enemy, new DanmakuPattern
            {
                PatternType = patternType,
                Shape = BulletShape.BallS,
                Color = BulletColor.Red,
                Speed = BULLET_SPEED,
                BulletCount = bulletCount,
                SpreadAngle = spreadAngle,
                SpiralSpeed = spiralSpeed,
                Accel = 0f,
                MaxSpeed = 0f,
                SpawnDelayFrames = 0
            });

            if (patternType == DanmakuPatternType.Spiral)
            {
                _em.AddComponentData(enemy, new DanmakuSpiralAngle { Value = 0f });
            }

            return enemy;
        }

        /// <summary>
        /// Creates a player entity for AIMED pattern tests.
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
        /// Advances time and updates the pattern system + ECB playback.
        /// </summary>
        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _patternSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        /// <summary>
        /// Counts entities that have BulletTag + EnemyBulletTag (bullets from factory).
        /// </summary>
        private int CountEnemyBullets()
        {
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.ReadOnly<EnemyBulletTag>());
            int count = query.CalculateEntityCount();
            query.Dispose();
            return count;
        }

        [Test]
        public void Straight_FiresSingleBulletWithBulletMotion()
        {
            // Arrange
            CreatePatternEnemy(DanmakuPatternType.Straight);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(1, CountEnemyBullets(),
                "STRAIGHT pattern should fire exactly 1 bullet");

            // Check BulletMotion
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>(),
                ComponentType.ReadOnly<EnemyBulletTag>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var motion = _em.GetComponentData<BulletMotion>(entities[0]);
                Assert.AreEqual(BULLET_SPEED, motion.Speed, 0.01f,
                    "Bullet speed should match pattern speed");
                // Down angle: -PI/2
                Assert.AreEqual(-math.PI * 0.5f, motion.Angle, 0.01f,
                    "STRAIGHT bullet should point downward (-PI/2)");
            }
            query.Dispose();
        }

        [Test]
        public void Fan_FiresMultipleBullets()
        {
            // Arrange
            int fanCount = 5;
            CreatePatternEnemy(DanmakuPatternType.Fan,
                bulletCount: fanCount, spreadAngle: 1.047f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(fanCount, CountEnemyBullets(),
                $"FAN pattern with BulletCount={fanCount} should fire {fanCount} bullets");
        }

        [Test]
        public void Fan_BulletsHaveSpreadAngles()
        {
            // Arrange
            CreatePatternEnemy(DanmakuPatternType.Fan,
                bulletCount: 3, spreadAngle: 1.047f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>(),
                ComponentType.ReadOnly<EnemyBulletTag>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                Assert.AreEqual(3, entities.Length);

                float prevAngle = float.NaN;
                bool hasDifferentAngles = false;
                for (int i = 0; i < entities.Length; i++)
                {
                    var motion = _em.GetComponentData<BulletMotion>(entities[i]);
                    if (!float.IsNaN(prevAngle) &&
                        math.abs(motion.Angle - prevAngle) > 0.01f)
                    {
                        hasDifferentAngles = true;
                    }
                    prevAngle = motion.Angle;
                }
                Assert.IsTrue(hasDifferentAngles,
                    "Fan bullets should have different angles (spread)");
            }
            query.Dispose();
        }

        [Test]
        public void Aimed_FiresAtPlayer()
        {
            // Arrange
            var playerPos = new float3(3f, -4f, 0f);
            var enemyPos = new float3(0f, 5f, 0f);
            CreatePlayer(pos: playerPos);
            CreatePatternEnemy(DanmakuPatternType.Aimed, pos: enemyPos);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(1, CountEnemyBullets(),
                "AIMED pattern should fire 1 bullet");

            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletMotion>(),
                ComponentType.ReadOnly<EnemyBulletTag>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var motion = _em.GetComponentData<BulletMotion>(entities[0]);
                // The spawn pos is enemyPos + (0, -0.5, 0)
                var spawnPos = enemyPos + new float3(0f, -0.5f, 0f);
                float expectedAngle = math.atan2(
                    playerPos.y - spawnPos.y, playerPos.x - spawnPos.x);
                Assert.AreEqual(expectedAngle, motion.Angle, 0.01f,
                    "AIMED bullet should point toward player");
            }
            query.Dispose();
        }

        [Test]
        public void Spiral_AngleIncrements()
        {
            // Arrange
            float spiralSpeed = 0.524f; // ~30 degrees
            var enemy = CreatePatternEnemy(DanmakuPatternType.Spiral,
                spiralSpeed: spiralSpeed);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(1, CountEnemyBullets(),
                "SPIRAL should fire 1 bullet per shot");

            var angle = _em.GetComponentData<DanmakuSpiralAngle>(enemy);
            Assert.AreEqual(spiralSpeed, angle.Value, 0.01f,
                "DanmakuSpiralAngle should increment by SpiralSpeed after firing");
        }

        [Test]
        public void Ring_FiresCorrectCount()
        {
            // Arrange
            int ringCount = 12;
            CreatePatternEnemy(DanmakuPatternType.Ring, bulletCount: ringCount);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(ringCount, CountEnemyBullets(),
                $"RING pattern should fire {ringCount} bullets");
        }

        [Test]
        public void Spread_FiresCorrectCount()
        {
            // Arrange
            int spreadCount = 7;
            CreatePatternEnemy(DanmakuPatternType.Spread,
                bulletCount: spreadCount, spreadAngle: 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(spreadCount, CountEnemyBullets(),
                $"SPREAD pattern should fire {spreadCount} bullets");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPatternEnemies()
        {
            // Arrange -- no enemies with DanmakuPattern

            // Act -- should not crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _patternSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.AreEqual(0, CountEnemyBullets(),
                "No bullets should be spawned when no pattern enemies exist");
        }

        [Test]
        public void BulletsHaveCorrectVisual()
        {
            // Arrange
            CreatePatternEnemy(DanmakuPatternType.Straight);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletVisual>(),
                ComponentType.ReadOnly<EnemyBulletTag>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var visual = _em.GetComponentData<BulletVisual>(entities[0]);
                Assert.AreEqual(BulletShape.BallS, visual.Shape);
                Assert.AreEqual(BulletColor.Red, visual.Color);
            }
            query.Dispose();
        }

        [Test]
        public void Cooldown_PreventsDoublefire()
        {
            // Arrange
            CreatePatternEnemy(DanmakuPatternType.Straight);

            // Act -- fire once, then update again immediately
            AdvanceTimeAndUpdate();
            AdvanceTimeAndUpdate();

            // Assert -- should still only have 1 bullet (cooldown hasn't expired)
            Assert.AreEqual(1, CountEnemyBullets(),
                "Should not fire again while cooldown is active");
        }
    }
}
