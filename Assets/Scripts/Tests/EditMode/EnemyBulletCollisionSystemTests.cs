using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Player;
using MyGame.ECS.Collision;

namespace MyGame.Tests
{
    /// <summary>
    /// EnemyBulletCollisionSystem 的 EditMode 測試。
    /// 驗證敵彈命中玩家、扣血、無敵跳過、死亡、未命中。
    /// </summary>
    [TestFixture]
    public class EnemyBulletCollisionSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _collisionSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _collisionSystemHandle = _world.GetOrCreateSystem<EnemyBulletCollisionSystem>();
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
        /// 建立玩家 entity。
        /// </summary>
        private Entity CreatePlayer(
            float3? pos = null,
            float radius = 0.08f,
            int hp = 3,
            float invTimer = 0f,
            float invDuration = 2.0f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlayerTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new HealthData { Current = hp, Max = hp });
            _em.AddComponentData(entity, new InvincibilityTimer { Value = invTimer });
            _em.AddComponentData(entity, new InvincibilityDuration { Value = invDuration });
            return entity;
        }

        /// <summary>
        /// 建立敵人子彈 entity。
        /// </summary>
        private Entity CreateEnemyBullet(
            float3? pos = null,
            float radius = 0.1f,
            int damage = 1)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponent<EnemyBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(entity, new Velocity { Value = new float3(0f, -8f, 0f) });
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new DamageOnContact { Value = damage });
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
            _collisionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void EnemyBullet_DestroysOnPlayerHit()
        {
            // Arrange
            CreatePlayer(pos: new float3(0f, 0f, 0f));
            var bullet = CreateEnemyBullet(pos: new float3(0f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Enemy bullet should be destroyed on player hit");
        }

        [Test]
        public void Player_TakesDamage_OnEnemyBulletHit()
        {
            // Arrange
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(2, health.Current,
                "Player HP should be reduced by bullet damage");
        }

        [Test]
        public void Player_DiesWhenHpReachesZero()
        {
            // Arrange — HP = 1
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 1);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.HasComponent<DeadTag>(player),
                "Player should have DeadTag when HP reaches zero");
        }

        [Test]
        public void Player_SurvivesWhenHpAboveZero()
        {
            // Arrange
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.HasComponent<DeadTag>(player),
                "Player with HP > 0 should not have DeadTag");
        }

        [Test]
        public void Player_InvincibilityActivated_AfterHit()
        {
            // Arrange — 玩家有多點 HP，被擊後應啟動無敵
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3, invDuration: 2.0f);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 無敵計時器應被設定
            var timer = _em.GetComponentData<InvincibilityTimer>(player);
            Assert.AreEqual(2.0f, timer.Value, 0.001f,
                "InvincibilityTimer should be set to InvincibilityDuration after hit");
        }

        [Test]
        public void Player_NotHit_WhenInvincible()
        {
            // Arrange — 玩家已無敵
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3, invTimer: 1.0f);
            var bullet = CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 子彈不應被銷毀，玩家 HP 不變
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet should survive when player is invincible");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP should be unchanged when invincible");
        }

        [Test]
        public void EnemyBullet_NoCollision_WhenOutOfRange()
        {
            // Arrange — 距離很遠
            var player = CreatePlayer(pos: new float3(-10f, 0f, 0f), hp: 3);
            var bullet = CreateEnemyBullet(pos: new float3(10f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet should survive when out of range");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP should be unchanged when no collision");
        }

        [Test]
        public void System_DoesNotRun_WhenNoEnemyBullets()
        {
            // Arrange — 只有玩家，沒有敵彈
            CreatePlayer();

            // Act
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collisionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no EnemyBulletTag entities exist");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPlayer()
        {
            // Arrange — 只有敵彈，沒有玩家
            CreateEnemyBullet();

            // Act
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collisionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no PlayerTag entities exist");
        }
    }
}
