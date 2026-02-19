using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;
using MyGame.ECS.Enemy;
using MyGame.ECS.Player;
using MyGame.ECS.Wave;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for BulletPatternSystem.
    /// Validates STRAIGHT, FAN, SPIRAL, and AIMED bullet patterns.
    /// </summary>
    [TestFixture]
    public class BulletPatternSystemTests
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
            _patternSystemHandle = _world.GetOrCreateSystem<BulletPatternSystem>();
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
        /// Creates a bullet prefab entity (non-linked, used by ECB.Instantiate).
        /// </summary>
        private Entity CreateBulletPrefab()
        {
            var prefab = _em.CreateEntity();
            _em.AddComponent<BulletTag>(prefab);
            _em.AddComponentData(prefab, new Velocity { Value = float3.zero });
            _em.AddComponentData(prefab, LocalTransform.FromPosition(float3.zero));
            _em.AddComponentData(prefab, new BulletLifetime { Value = 5f });
            _em.AddComponentData(prefab, new CollisionRadius { Value = 0.12f });
            _em.AddComponentData(prefab, new DamageOnContact { Value = 1 });
            return prefab;
        }

        /// <summary>
        /// Creates an enemy entity with BulletPatternData and shooting components.
        /// Cooldown is set to 0 so it fires immediately on next update.
        /// </summary>
        private Entity CreatePatternEnemy(
            int patternType,
            int bulletCount = 1,
            float spreadAngle = 60f,
            float spiralSpeed = 15f,
            float3? pos = null)
        {
            var bulletPrefab = CreateBulletPrefab();
            var enemy = _em.CreateEntity();

            _em.AddComponent<EnemyTag>(enemy);
            _em.AddComponentData(enemy, LocalTransform.FromPosition(
                pos ?? new float3(0f, 5f, 0f)));
            _em.AddComponentData(enemy, new EnemyVelocity
            {
                Value = new float3(0f, -3f, 0f)
            });
            _em.AddComponentData(enemy, new EnemyBulletPrefabRef
            {
                Value = bulletPrefab
            });
            _em.AddComponentData(enemy, new EnemyShootCooldown
            {
                Timer = 0f, // ready to fire immediately
                Duration = 1f
            });
            _em.AddComponentData(enemy, new EnemyBulletSpeedData
            {
                Value = BULLET_SPEED
            });
            _em.AddComponentData(enemy, new BulletPatternData
            {
                PatternType = patternType,
                BulletCount = bulletCount,
                SpreadAngle = spreadAngle,
                SpiralSpeed = spiralSpeed
            });

            if (patternType == BulletPatternData.SPIRAL)
            {
                _em.AddComponentData(enemy, new SpiralAngle { Value = 0f });
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
        /// Counts entities that have EnemyBulletTag (bullets spawned by the system).
        /// </summary>
        private int CountEnemyBullets()
        {
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyBulletTag>());
            int count = query.CalculateEntityCount();
            query.Dispose();
            return count;
        }

        [Test]
        public void Straight_FiresSingleBulletDown()
        {
            // Arrange
            CreatePatternEnemy(BulletPatternData.STRAIGHT);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(1, CountEnemyBullets(),
                "STRAIGHT pattern should fire exactly 1 bullet");

            // Check velocity points downward
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyBulletTag>(),
                ComponentType.ReadOnly<Velocity>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var vel = _em.GetComponentData<Velocity>(entities[0]);
                Assert.Less(vel.Value.y, 0f,
                    "STRAIGHT bullet should have negative Y velocity (downward)");
                Assert.AreEqual(0f, vel.Value.x, 0.001f,
                    "STRAIGHT bullet should have zero X velocity");
            }
            query.Dispose();
        }

        [Test]
        public void Fan_FiresMultipleBullets()
        {
            // Arrange
            int fanCount = 5;
            CreatePatternEnemy(BulletPatternData.FAN, bulletCount: fanCount, spreadAngle: 60f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(fanCount, CountEnemyBullets(),
                $"FAN pattern with BulletCount={fanCount} should fire {fanCount} bullets");
        }

        [Test]
        public void Fan_BulletsHaveSpreadVelocity()
        {
            // Arrange
            CreatePatternEnemy(BulletPatternData.FAN, bulletCount: 3, spreadAngle: 60f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — bullets should have different X velocities
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyBulletTag>(),
                ComponentType.ReadOnly<Velocity>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                Assert.AreEqual(3, entities.Length, "Should have 3 fan bullets");

                float prevX = float.NaN;
                bool hasDifferentX = false;
                for (int i = 0; i < entities.Length; i++)
                {
                    var vel = _em.GetComponentData<Velocity>(entities[i]);
                    // All bullets should point generally downward
                    Assert.Less(vel.Value.y, 0f,
                        $"Fan bullet {i} should have negative Y velocity");

                    if (!float.IsNaN(prevX) && math.abs(vel.Value.x - prevX) > 0.01f)
                    {
                        hasDifferentX = true;
                    }
                    prevX = vel.Value.x;
                }
                Assert.IsTrue(hasDifferentX,
                    "Fan bullets should have different X velocities (spread)");
            }
            query.Dispose();
        }

        [Test]
        public void Aimed_FiresAtPlayer()
        {
            // Arrange — player below and to the right of the enemy
            var playerPos = new float3(3f, -4f, 0f);
            var enemyPos = new float3(0f, 5f, 0f);
            CreatePlayer(pos: playerPos);
            CreatePatternEnemy(BulletPatternData.AIMED, pos: enemyPos);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(1, CountEnemyBullets(),
                "AIMED pattern should fire 1 bullet");

            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyBulletTag>(),
                ComponentType.ReadOnly<Velocity>());
            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var vel = _em.GetComponentData<Velocity>(entities[0]);
                // Bullet should be heading toward player (right and down)
                Assert.Greater(vel.Value.x, 0f,
                    "AIMED bullet should have positive X velocity (toward player)");
                Assert.Less(vel.Value.y, 0f,
                    "AIMED bullet should have negative Y velocity (toward player)");
            }
            query.Dispose();
        }

        [Test]
        public void Spiral_AngleIncrements()
        {
            // Arrange
            float spiralSpeed = 30f;
            var enemy = CreatePatternEnemy(BulletPatternData.SPIRAL, spiralSpeed: spiralSpeed);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var angle = _em.GetComponentData<SpiralAngle>(enemy);
            Assert.AreEqual(spiralSpeed, angle.Value, 0.01f,
                "SpiralAngle should increment by SpiralSpeed after firing");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPatternEnemies()
        {
            // Arrange — no enemies with BulletPatternData

            // Act — should not crash
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
    }
}
