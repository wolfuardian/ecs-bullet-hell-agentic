using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bomb;
using MyGame.ECS.Collision;
using MyGame.ECS.Enemy;
using MyGame.ECS.Player;

namespace MyGame.Tests
{
    /// <summary>
    /// BombSystem 的 EditMode 測試。
    /// 驗證炸彈啟動、庫存扣除、冷卻、清除敵彈、殲滅敵人、無敵、計時器。
    /// </summary>
    [TestFixture]
    public class BombSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _bombSystemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _bombSystemHandle = _world.GetOrCreateSystem<BombSystem>();
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
        /// 建立玩家 entity，含 PlayerTag、BombData、InvincibilityTimer。
        /// </summary>
        private Entity CreatePlayer(
            int bombStock = 3,
            float cooldownTimer = 0f,
            float cooldownDuration = 1.0f,
            float bombDuration = 3.0f)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlayerTag>(entity);
            _em.AddComponentData(entity, new BombData
            {
                Stock = bombStock,
                CooldownTimer = cooldownTimer,
                CooldownDuration = cooldownDuration,
                BombDuration = bombDuration
            });
            _em.AddComponentData(entity, new InvincibilityTimer { Value = 0f });
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            return entity;
        }

        /// <summary>
        /// 建立 PlayerInputData singleton entity。
        /// </summary>
        private Entity CreateInputSingleton(bool bombPressed = false)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new PlayerInputData
            {
                MoveInput = float2.zero,
                ShootHeld = false,
                FocusHeld = false,
                BombPressed = bombPressed
            });
            return entity;
        }

        /// <summary>
        /// 建立敵人子彈 entity。
        /// </summary>
        private Entity CreateEnemyBullet(float3? pos = null)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            return entity;
        }

        /// <summary>
        /// 建立敵人 entity。
        /// </summary>
        private Entity CreateEnemy(float3? pos = null)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            return entity;
        }

        /// <summary>
        /// 推進時間並更新系統。
        /// </summary>
        private void AdvanceTimeAndUpdate(float dt = TEST_DELTA_TIME)
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + dt,
                deltaTime: dt));
            _bombSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void Bomb_ActivatesWhenBombPressedAndStockPositive()
        {
            // Arrange
            var player = CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(2, bombData.Stock,
                "Bomb stock should decrement by 1 after activation");
            Assert.IsTrue(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should be added to player after bomb activation");
        }

        [Test]
        public void Bomb_DoesNotActivate_WhenStockZero()
        {
            // Arrange
            var player = CreatePlayer(bombStock: 0);
            CreateInputSingleton(bombPressed: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(0, bombData.Stock,
                "Stock should remain 0");
            Assert.IsFalse(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should NOT be added when stock is 0");
        }

        [Test]
        public void Bomb_DoesNotActivate_WhenOnCooldown()
        {
            // Arrange — cooldown is active
            var player = CreatePlayer(bombStock: 3, cooldownTimer: 0.5f);
            CreateInputSingleton(bombPressed: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(3, bombData.Stock,
                "Stock should remain unchanged when on cooldown");
            Assert.IsFalse(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should NOT be added when on cooldown");
        }

        [Test]
        public void Bomb_DestroysAllEnemyBullets()
        {
            // Arrange
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            var bullet1 = CreateEnemyBullet(new float3(1f, 0f, 0f));
            var bullet2 = CreateEnemyBullet(new float3(-1f, 0f, 0f));
            var bullet3 = CreateEnemyBullet(new float3(0f, 5f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsFalse(_em.Exists(bullet1),
                "Enemy bullet 1 should be destroyed by bomb");
            Assert.IsFalse(_em.Exists(bullet2),
                "Enemy bullet 2 should be destroyed by bomb");
            Assert.IsFalse(_em.Exists(bullet3),
                "Enemy bullet 3 should be destroyed by bomb");
        }

        [Test]
        public void Bomb_KillsAllEnemies()
        {
            // Arrange
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            var enemy1 = CreateEnemy(new float3(1f, 3f, 0f));
            var enemy2 = CreateEnemy(new float3(-1f, 3f, 0f));
            var enemy3 = CreateEnemy(new float3(0f, 5f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.HasComponent<DeadTag>(enemy1),
                "Enemy 1 should have DeadTag after bomb");
            Assert.IsTrue(_em.HasComponent<DeadTag>(enemy2),
                "Enemy 2 should have DeadTag after bomb");
            Assert.IsTrue(_em.HasComponent<DeadTag>(enemy3),
                "Enemy 3 should have DeadTag after bomb");
        }

        [Test]
        public void Bomb_GrantsPlayerInvincibility()
        {
            // Arrange
            var player = CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var invTimer = _em.GetComponentData<InvincibilityTimer>(player);
            Assert.Greater(invTimer.Value, 0f,
                "InvincibilityTimer should be > 0 after bomb activation");
        }

        [Test]
        public void BombActive_TimerDecrementsOverTime()
        {
            // Arrange — manually add BombActiveData to simulate active bomb
            var player = CreatePlayer(bombStock: 3);
            _em.AddComponentData(player, new BombActiveData { Timer = 3.0f });
            CreateInputSingleton(bombPressed: false);

            // Act
            AdvanceTimeAndUpdate(0.5f);

            // Assert
            var bombActive = _em.GetComponentData<BombActiveData>(player);
            Assert.AreEqual(2.5f, bombActive.Timer, 0.001f,
                "BombActiveData.Timer should decrement by delta time");
        }

        [Test]
        public void BombActive_RemovedWhenTimerExpires()
        {
            // Arrange — timer is about to expire
            var player = CreatePlayer(bombStock: 3);
            _em.AddComponentData(player, new BombActiveData { Timer = 0.01f });
            CreateInputSingleton(bombPressed: false);

            // Act — advance more than remaining timer
            AdvanceTimeAndUpdate(0.1f);

            // Assert
            Assert.IsFalse(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should be removed when timer expires");
        }

        [Test]
        public void BombCooldown_DecrementsOverTime()
        {
            // Arrange — cooldown is active, no bomb press
            var player = CreatePlayer(bombStock: 3, cooldownTimer: 1.0f);
            CreateInputSingleton(bombPressed: false);

            // Act
            AdvanceTimeAndUpdate(0.5f);

            // Assert
            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(0.5f, bombData.CooldownTimer, 0.001f,
                "CooldownTimer should decrement by delta time");
        }
    }
}
