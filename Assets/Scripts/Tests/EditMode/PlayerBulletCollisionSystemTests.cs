using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Enemy;
using MyGame.ECS.Collision;

namespace MyGame.Tests
{
    /// <summary>
    /// PlayerBulletCollisionSystem 的 EditMode 測試。
    /// 驗證玩家子彈命中敵人、扣血、擊殺、未命中、多彈累積。
    /// </summary>
    [TestFixture]
    public class PlayerBulletCollisionSystemTests
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
            _collisionSystemHandle = _world.GetOrCreateSystem<PlayerBulletCollisionSystem>();
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
        /// 建立玩家子彈 entity（非 Prefab）。
        /// </summary>
        private Entity CreatePlayerBullet(
            float3? pos = null,
            float radius = 0.12f,
            int damage = 1)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponent<PlayerBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new Velocity { Value = new float3(0f, 20f, 0f) });
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new DamageOnContact { Value = damage });
            return entity;
        }

        /// <summary>
        /// 建立敵人 entity（非 Prefab）。
        /// </summary>
        private Entity CreateEnemy(
            float3? pos = null,
            float radius = 0.4f,
            int hp = 3)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(entity, new EnemyVelocity { Value = new float3(0f, -3f, 0f) });
            _em.AddComponentData(entity, new CollisionRadius { Value = radius });
            _em.AddComponentData(entity, new HealthData { Current = hp, Max = hp });
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
        public void PlayerBullet_DestroysOnEnemyHit()
        {
            // Arrange — 子彈和敵人在同一位置
            var bullet = CreatePlayerBullet(pos: new float3(0f, 3f, 0f));
            CreateEnemy(pos: new float3(0f, 3f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Player bullet should be destroyed on enemy hit");
        }

        [Test]
        public void Enemy_TakesDamage_OnPlayerBulletHit()
        {
            // Arrange
            var bullet = CreatePlayerBullet(pos: new float3(0f, 3f, 0f), damage: 1);
            var enemy = CreateEnemy(pos: new float3(0f, 3f, 0f), hp: 3);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var health = _em.GetComponentData<HealthData>(enemy);
            Assert.AreEqual(2, health.Current,
                "Enemy HP should be reduced by bullet damage");
        }

        [Test]
        public void Enemy_DiesWhenHpReachesZero()
        {
            // Arrange — 敵人 HP = 1，子彈傷害 = 1
            CreatePlayerBullet(pos: new float3(0f, 3f, 0f), damage: 1);
            var enemy = CreateEnemy(pos: new float3(0f, 3f, 0f), hp: 1);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 敵人應被加上 DeadTag
            Assert.IsTrue(_em.HasComponent<DeadTag>(enemy),
                "Enemy should have DeadTag when HP reaches zero");
        }

        [Test]
        public void Enemy_SurvivesWhenHpAboveZero()
        {
            // Arrange — 敵人 HP = 3，子彈傷害 = 1
            CreatePlayerBullet(pos: new float3(0f, 3f, 0f), damage: 1);
            var enemy = CreateEnemy(pos: new float3(0f, 3f, 0f), hp: 3);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.HasComponent<DeadTag>(enemy),
                "Enemy with HP > 0 after hit should not have DeadTag");
        }

        [Test]
        public void PlayerBullet_NoCollision_WhenOutOfRange()
        {
            // Arrange — 子彈和敵人距離很遠
            var bullet = CreatePlayerBullet(pos: new float3(-10f, 0f, 0f));
            var enemy = CreateEnemy(pos: new float3(10f, 0f, 0f), hp: 3);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet should survive when out of range");
            var health = _em.GetComponentData<HealthData>(enemy);
            Assert.AreEqual(3, health.Current,
                "Enemy HP should be unchanged when no collision");
        }

        [Test]
        public void MultipleBullets_HitSameEnemy_AccumulateDamage()
        {
            // Arrange — 兩顆子彈在同一位置命中同一敵人
            var bullet1 = CreatePlayerBullet(pos: new float3(0f, 3f, 0f), damage: 1);
            var bullet2 = CreatePlayerBullet(pos: new float3(0f, 3f, 0f), damage: 1);
            var enemy = CreateEnemy(pos: new float3(0f, 3f, 0f), hp: 3);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 兩顆子彈都應被銷毀
            Assert.IsFalse(_em.Exists(bullet1), "First bullet should be destroyed");
            Assert.IsFalse(_em.Exists(bullet2), "Second bullet should be destroyed");

            // 敵人 HP 應扣 2
            var health = _em.GetComponentData<HealthData>(enemy);
            Assert.AreEqual(1, health.Current,
                "Enemy should take accumulated damage from multiple bullets");
        }

        [Test]
        public void PlayerBullet_IgnoresDeadEnemy()
        {
            // Arrange — 敵人已有 DeadTag
            var bullet = CreatePlayerBullet(pos: new float3(0f, 3f, 0f));
            var enemy = CreateEnemy(pos: new float3(0f, 3f, 0f), hp: 3);
            _em.AddComponent<DeadTag>(enemy);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 子彈不應被銷毀（跳過已死敵人）
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet should not collide with dead enemy");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPlayerBullets()
        {
            // Arrange — 只有敵人，沒有玩家子彈
            CreateEnemy(pos: new float3(0f, 3f, 0f));

            // Act — 不應 crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collisionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no PlayerBulletTag entities exist");
        }

        [Test]
        public void System_DoesNotRun_WhenNoEnemies()
        {
            // Arrange — 只有子彈，沒有敵人
            CreatePlayerBullet(pos: new float3(0f, 3f, 0f));

            // Act — 不應 crash
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
