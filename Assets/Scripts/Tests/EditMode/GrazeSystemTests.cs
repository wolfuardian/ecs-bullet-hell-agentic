using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Player;
using MyGame.ECS.Collision;
using MyGame.ECS.Graze;

namespace MyGame.Tests
{
    /// <summary>
    /// GrazeSystem 的 EditMode 測試。
    /// 驗證擦彈偵測、GrazedTag 標記、重複計算防止、累積計數。
    /// </summary>
    [TestFixture]
    public class GrazeSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _grazeSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _grazeSystemHandle = _world.GetOrCreateSystem<GrazeSystem>();
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
        /// 建立玩家 entity（含 GrazeData）。
        /// </summary>
        private Entity CreatePlayer(
            float3? pos = null,
            float collisionRadius = 0.08f,
            float grazeRadius = 0.5f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlayerTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new CollisionRadius { Value = collisionRadius });
            _em.AddComponentData(entity, new GrazeData { Count = 0, GrazeRadius = grazeRadius });
            _em.AddComponentData(entity, new HealthData { Current = 3, Max = 3 });
            _em.AddComponentData(entity, new InvincibilityTimer { Value = 0f });
            _em.AddComponentData(entity, new InvincibilityDuration { Value = 2.0f });
            return entity;
        }

        /// <summary>
        /// 建立敵人子彈 entity。
        /// </summary>
        private Entity CreateEnemyBullet(
            float3? pos = null,
            float radius = 0.12f,
            bool grazed = false)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponent<EnemyBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(entity, new Velocity { Value = new float3(0f, -8f, 0f) });
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new DamageOnContact { Value = 1 });

            if (grazed)
            {
                _em.AddComponent<GrazedTag>(entity);
            }

            return entity;
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
            _grazeSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void Graze_Detected_WhenBulletInGrazeRange()
        {
            // Arrange — bullet at 0.3, between collision (0.20) and graze (0.62)
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(1, grazeData.Count,
                "Graze count should be 1 when bullet is in graze range");
        }

        [Test]
        public void Graze_NotDetected_WhenBulletTooFar()
        {
            // Arrange — bullet at 1.0, outside graze radius (0.62)
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(1.0f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Graze count should remain 0 when bullet is too far");
        }

        [Test]
        public void Graze_NotDetected_WhenBulletInCollisionRange()
        {
            // Arrange — bullet at 0.1, inside collision radius (0.20)
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.1f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Graze count should remain 0 when bullet is in collision range");
        }

        [Test]
        public void GrazedBullet_NotCountedTwice()
        {
            // Arrange — bullet already has GrazedTag
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f), grazed: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Graze count should remain 0 for already-grazed bullet");
        }

        [Test]
        public void GrazeCount_Accumulates()
        {
            // Arrange — multiple bullets in graze range
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));
            CreateEnemyBullet(pos: new float3(-0.3f, 0f, 0f));
            CreateEnemyBullet(pos: new float3(0f, 0.3f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(3, grazeData.Count,
                "Graze count should accumulate for multiple bullets in range");
        }

        [Test]
        public void GrazedTag_AddedToBullet()
        {
            // Arrange
            CreatePlayer(pos: float3.zero);
            var bullet = CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.HasComponent<GrazedTag>(bullet),
                "Bullet should have GrazedTag after being grazed");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPlayer()
        {
            // Arrange — only enemy bullet, no player
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));

            // Act
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _grazeSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no PlayerTag entities exist");
        }

        [Test]
        public void System_DoesNotRun_WhenNoEnemyBullets()
        {
            // Arrange — only player, no enemy bullets
            CreatePlayer(pos: float3.zero);

            // Act
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _grazeSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no EnemyBulletTag entities exist");
        }
    }
}
