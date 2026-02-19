using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;
using MyGame.ECS.Bullet;

namespace MyGame.Tests
{
    /// <summary>
    /// EnemyBulletSpawnSystem 的 EditMode 測試。
    /// 驗證敵人射擊冷卻、子彈生成、BulletTag 復用、速度方向。
    /// </summary>
    [TestFixture]
    public class EnemyBulletSpawnSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _bulletSpawnSystemHandle;
        private SystemHandle _ecbSystemHandle;

        /// <summary>測試用固定 DeltaTime（1/60 秒）。</summary>
        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            _bulletSpawnSystemHandle = _world.GetOrCreateSystem<EnemyBulletSpawnSystem>();
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
        /// 建立一顆「子彈 Prefab entity」—— 測試中用手動建立的 entity 模擬。
        /// 真實遊戲中由 BulletAuthoring Baker 建立。
        /// </summary>
        private Entity CreateBulletPrefabEntity()
        {
            var prefab = _em.CreateEntity();
            _em.AddComponentData(prefab, new BulletTag());
            _em.AddComponentData(prefab, LocalTransform.FromPosition(float3.zero));
            _em.AddComponentData(prefab, new Velocity { Value = float3.zero });
            _em.AddComponentData(prefab, new BulletLifetime { Value = 5f });
            // 標記為 Prefab 讓 Query 不會抓到它
            _em.AddComponent<Prefab>(prefab);
            return prefab;
        }

        /// <summary>
        /// 建立有完整射擊能力的 Enemy entity。
        /// </summary>
        private Entity CreateShootingEnemy(
            float3? pos = null,
            float cooldownTimer = 0f,
            float cooldownDuration = 1f,
            float bulletSpeed = 8f,
            Entity? bulletPrefab = null)
        {
            var prefab = bulletPrefab ?? CreateBulletPrefabEntity();

            var enemy = _em.CreateEntity();
            _em.AddComponentData(enemy, new EnemyTag());
            _em.AddComponentData(enemy, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(enemy, new EnemyVelocity { Value = new float3(0f, -3f, 0f) });
            _em.AddComponentData(enemy, new EnemyBulletPrefabRef { Value = prefab });
            _em.AddComponentData(enemy, new EnemyShootCooldown
            {
                Timer = cooldownTimer,
                Duration = cooldownDuration
            });
            _em.AddComponentData(enemy, new EnemyBulletSpeedData { Value = bulletSpeed });
            return enemy;
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
            _bulletSpawnSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        /// <summary>
        /// 查詢場上所有非 Prefab 的 BulletTag entity 數量。
        /// </summary>
        private int CountActiveBullets()
        {
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.Exclude<Prefab>());
            return query.CalculateEntityCount();
        }

        [Test]
        public void EnemyFiresBullet_WhenCooldownExpires()
        {
            // Arrange — cooldown 已歸零，應立即射擊
            CreateShootingEnemy(cooldownTimer: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(1, CountActiveBullets(),
                "One bullet should be spawned when cooldown expires");
        }

        [Test]
        public void EnemyDoesNotFire_WhenCooldownRemaining()
        {
            // Arrange — cooldown 還有很長時間
            CreateShootingEnemy(cooldownTimer: 5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.AreEqual(0, CountActiveBullets(),
                "No bullets should be spawned while cooldown is active");
        }

        [Test]
        public void EnemyBullet_HasBulletTag()
        {
            // Arrange
            CreateShootingEnemy(cooldownTimer: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 子彈應有 BulletTag（復用既有子彈系統）
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.Exclude<Prefab>());
            Assert.AreEqual(1, query.CalculateEntityCount(),
                "Spawned enemy bullet should have BulletTag");
        }

        [Test]
        public void EnemyBullet_HasNegativeYVelocity()
        {
            // Arrange
            var bulletSpeed = 8f;
            CreateShootingEnemy(cooldownTimer: 0f, bulletSpeed: bulletSpeed);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 敵彈往 -Y 方向飛
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.ReadOnly<Velocity>(),
                ComponentType.Exclude<Prefab>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            Assert.AreEqual(1, entities.Length);

            var vel = _em.GetComponentData<Velocity>(entities[0]);
            Assert.AreEqual(0f, vel.Value.x, 0.001f, "Bullet X velocity should be 0");
            Assert.Less(vel.Value.y, 0f, "Bullet should have negative Y velocity (downward)");
            Assert.AreEqual(-bulletSpeed, vel.Value.y, 0.001f,
                "Bullet Y speed should match EnemyBulletSpeedData");
            entities.Dispose();
        }

        [Test]
        public void EnemyBullet_SpawnedBelowEnemyPosition()
        {
            // Arrange
            var enemyPos = new float3(1f, 3f, 0f);
            CreateShootingEnemy(pos: enemyPos, cooldownTimer: 0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 子彈生成在敵人下方 0.5 的位置
            var query = _em.CreateEntityQuery(
                ComponentType.ReadOnly<BulletTag>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<Prefab>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            Assert.AreEqual(1, entities.Length);

            var bulletPos = _em.GetComponentData<LocalTransform>(entities[0]).Position;
            Assert.AreEqual(enemyPos.x, bulletPos.x, 0.001f, "Bullet X should match enemy X");
            Assert.AreEqual(enemyPos.y - 0.5f, bulletPos.y, 0.001f,
                "Bullet Y should be 0.5 below enemy");
            entities.Dispose();
        }

        [Test]
        public void EnemyCooldown_ResetsAfterFiring()
        {
            // Arrange
            var cooldownDuration = 1.5f;
            var enemy = CreateShootingEnemy(cooldownTimer: 0f, cooldownDuration: cooldownDuration);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 冷卻應被重置為 Duration
            var cd = _em.GetComponentData<EnemyShootCooldown>(enemy);
            Assert.Greater(cd.Timer, 0f, "Cooldown timer should be reset after firing");
            // 注意：重置後可能又被扣了一幀的 dt，所以檢查接近 Duration
            Assert.AreEqual(cooldownDuration, cd.Timer, TEST_DELTA_TIME + 0.001f,
                "Cooldown timer should be approximately equal to Duration after reset");
        }

        [Test]
        public void MultipleEnemies_FireIndependently()
        {
            // Arrange — 兩隻敵人：一隻冷卻好了，一隻還在冷卻
            CreateShootingEnemy(pos: new float3(-1f, 3f, 0f), cooldownTimer: 0f);
            CreateShootingEnemy(pos: new float3(1f, 3f, 0f), cooldownTimer: 5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 只有一顆子彈（只有第一隻敵人射擊）
            Assert.AreEqual(1, CountActiveBullets(),
                "Only one enemy should fire (the one with expired cooldown)");
        }
    }
}
