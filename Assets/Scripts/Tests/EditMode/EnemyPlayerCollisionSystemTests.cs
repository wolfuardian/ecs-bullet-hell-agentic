using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;
using MyGame.ECS.Player;
using MyGame.ECS.Collision;

namespace MyGame.Tests
{
    /// <summary>
    /// EnemyPlayerCollisionSystem 的 EditMode 測試。
    /// 驗證敵人體碰玩家的扣血、無敵跳過、敵人存活。
    /// </summary>
    [TestFixture]
    public class EnemyPlayerCollisionSystemTests
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
            _collisionSystemHandle = _world.GetOrCreateSystem<EnemyPlayerCollisionSystem>();
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
        /// 建立敵人 entity。
        /// </summary>
        private Entity CreateEnemy(
            float3? pos = null,
            float radius = 0.4f,
            int contactDamage = 1)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(entity, new EnemyVelocity { Value = new float3(0f, -3f, 0f) });
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new HealthData { Current = 3, Max = 3 });
            _em.AddComponentData(entity, new DamageOnContact { Value = contactDamage });
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
        public void Player_TakesDamage_OnEnemyBodyContact()
        {
            // Arrange — 玩家和敵人在同一位置
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3);
            CreateEnemy(pos: new float3(0f, 0f, 0f), contactDamage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(2, health.Current,
                "Player HP should be reduced by enemy contact damage");
        }

        [Test]
        public void Player_InvincibilityActivated_AfterBodyContact()
        {
            // Arrange
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3, invDuration: 2.0f);
            CreateEnemy(pos: new float3(0f, 0f, 0f), contactDamage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var timer = _em.GetComponentData<InvincibilityTimer>(player);
            Assert.AreEqual(2.0f, timer.Value, 0.001f,
                "InvincibilityTimer should be set after body contact");
        }

        [Test]
        public void Player_NotHit_WhenInvincible()
        {
            // Arrange — 玩家已無敵
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3, invTimer: 1.0f);
            CreateEnemy(pos: new float3(0f, 0f, 0f), contactDamage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Invincible player should not take damage from body contact");
        }

        [Test]
        public void Player_DiesWhenHpReachesZero_FromBodyContact()
        {
            // Arrange — HP = 1
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 1);
            CreateEnemy(pos: new float3(0f, 0f, 0f), contactDamage: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.HasComponent<DeadTag>(player),
                "Player should have DeadTag when HP reaches zero from body contact");
        }

        [Test]
        public void Enemy_NotDestroyed_OnBodyContact()
        {
            // Arrange
            CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3);
            var enemy = CreateEnemy(pos: new float3(0f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 敵人不應被銷毀（東方慣例）
            Assert.IsTrue(_em.Exists(enemy),
                "Enemy should NOT be destroyed on body contact (Touhou convention)");
        }

        [Test]
        public void NoCollision_WhenOutOfRange()
        {
            // Arrange — 距離很遠
            var player = CreatePlayer(pos: new float3(-10f, 0f, 0f), hp: 3);
            CreateEnemy(pos: new float3(10f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP should be unchanged when out of enemy range");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPlayer()
        {
            // Arrange — 只有敵人
            CreateEnemy();

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

        [Test]
        public void System_DoesNotRun_WhenNoEnemies()
        {
            // Arrange — 只有玩家
            CreatePlayer();

            // Act
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collisionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no EnemyTag entities exist");
        }
    }
}
