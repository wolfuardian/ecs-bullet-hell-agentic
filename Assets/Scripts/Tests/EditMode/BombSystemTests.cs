using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bomb;
using MyGame.ECS.Collision;
using MyGame.ECS.Danmaku;
using MyGame.ECS.Enemy;
using MyGame.ECS.Player;

namespace MyGame.Tests
{
    /// <summary>
    /// BombSystem EditMode tests.
    /// Validates bomb activation, stock deduction, cooldown, bullet clearing
    /// (including bomb-immune handling), enemy killing, invincibility, and timer.
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
        /// Create a player entity with PlayerTag, BombData, InvincibilityTimer.
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
        /// Create a PlayerInputData singleton entity.
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
        /// Create a regular enemy bullet entity (no BulletFlags).
        /// </summary>
        private Entity CreateEnemyBullet(float3? pos = null)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            return entity;
        }

        /// <summary>
        /// Create an enemy bullet with BulletFlags.
        /// </summary>
        private Entity CreateFlaggedEnemyBullet(float3? pos = null, bool bombImmune = false)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(entity, new BulletFlags
            {
                Value = bombImmune ? BulletFlags.BOMB_IMMUNE : (byte)0
            });
            return entity;
        }

        /// <summary>
        /// Create an enemy entity.
        /// </summary>
        private Entity CreateEnemy(float3? pos = null)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            return entity;
        }

        /// <summary>
        /// Advance time and update systems.
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

        // ==========================================
        // Existing bomb activation tests
        // ==========================================

        [Test]
        public void Bomb_ActivatesWhenBombPressedAndStockPositive()
        {
            var player = CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            AdvanceTimeAndUpdate();

            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(2, bombData.Stock,
                "Bomb stock should decrement by 1 after activation");
            Assert.IsTrue(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should be added to player after bomb activation");
        }

        [Test]
        public void Bomb_DoesNotActivate_WhenStockZero()
        {
            var player = CreatePlayer(bombStock: 0);
            CreateInputSingleton(bombPressed: true);

            AdvanceTimeAndUpdate();

            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(0, bombData.Stock,
                "Stock should remain 0");
            Assert.IsFalse(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should NOT be added when stock is 0");
        }

        [Test]
        public void Bomb_DoesNotActivate_WhenOnCooldown()
        {
            var player = CreatePlayer(bombStock: 3, cooldownTimer: 0.5f);
            CreateInputSingleton(bombPressed: true);

            AdvanceTimeAndUpdate();

            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(3, bombData.Stock,
                "Stock should remain unchanged when on cooldown");
            Assert.IsFalse(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should NOT be added when on cooldown");
        }

        [Test]
        public void Bomb_DestroysAllEnemyBullets()
        {
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            var bullet1 = CreateEnemyBullet(new float3(1f, 0f, 0f));
            var bullet2 = CreateEnemyBullet(new float3(-1f, 0f, 0f));
            var bullet3 = CreateEnemyBullet(new float3(0f, 5f, 0f));

            AdvanceTimeAndUpdate();

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
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            var enemy1 = CreateEnemy(new float3(1f, 3f, 0f));
            var enemy2 = CreateEnemy(new float3(-1f, 3f, 0f));
            var enemy3 = CreateEnemy(new float3(0f, 5f, 0f));

            AdvanceTimeAndUpdate();

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
            var player = CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            AdvanceTimeAndUpdate();

            var invTimer = _em.GetComponentData<InvincibilityTimer>(player);
            Assert.Greater(invTimer.Value, 0f,
                "InvincibilityTimer should be > 0 after bomb activation");
        }

        [Test]
        public void BombActive_TimerDecrementsOverTime()
        {
            var player = CreatePlayer(bombStock: 3);
            _em.AddComponentData(player, new BombActiveData { Timer = 3.0f });
            CreateInputSingleton(bombPressed: false);

            AdvanceTimeAndUpdate(0.5f);

            var bombActive = _em.GetComponentData<BombActiveData>(player);
            Assert.AreEqual(2.5f, bombActive.Timer, 0.001f,
                "BombActiveData.Timer should decrement by delta time");
        }

        [Test]
        public void BombActive_RemovedWhenTimerExpires()
        {
            var player = CreatePlayer(bombStock: 3);
            _em.AddComponentData(player, new BombActiveData { Timer = 0.01f });
            CreateInputSingleton(bombPressed: false);

            AdvanceTimeAndUpdate(0.1f);

            Assert.IsFalse(_em.HasComponent<BombActiveData>(player),
                "BombActiveData should be removed when timer expires");
        }

        [Test]
        public void BombCooldown_DecrementsOverTime()
        {
            var player = CreatePlayer(bombStock: 3, cooldownTimer: 1.0f);
            CreateInputSingleton(bombPressed: false);

            AdvanceTimeAndUpdate(0.5f);

            var bombData = _em.GetComponentData<BombData>(player);
            Assert.AreEqual(0.5f, bombData.CooldownTimer, 0.001f,
                "CooldownTimer should decrement by delta time");
        }

        // ==========================================
        // BulletFlags / Bomb-immune tests
        // ==========================================

        [Test]
        public void Bomb_SparesBombImmuneBullets()
        {
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            var normalBullet = CreateEnemyBullet(new float3(1f, 0f, 0f));
            var immuneBullet = CreateFlaggedEnemyBullet(new float3(-1f, 0f, 0f), bombImmune: true);

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(normalBullet),
                "Normal bullet should be destroyed by bomb");
            Assert.IsTrue(_em.Exists(immuneBullet),
                "Bomb-immune bullet should survive bomb");
        }

        [Test]
        public void Bomb_DestroysFlaggedBullet_WhenNotBombImmune()
        {
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            // Has BulletFlags but BombImmune is false
            var flaggedBullet = CreateFlaggedEnemyBullet(new float3(1f, 0f, 0f), bombImmune: false);

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(flaggedBullet),
                "Flagged bullet without bomb-immune flag should be destroyed by bomb");
        }

        [Test]
        public void Bomb_MultipleImmuneBullets_AllSurvive()
        {
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            var immune1 = CreateFlaggedEnemyBullet(new float3(1f, 0f, 0f), bombImmune: true);
            var immune2 = CreateFlaggedEnemyBullet(new float3(-1f, 0f, 0f), bombImmune: true);
            var immune3 = CreateFlaggedEnemyBullet(new float3(0f, 5f, 0f), bombImmune: true);

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.Exists(immune1),
                "Bomb-immune bullet 1 should survive");
            Assert.IsTrue(_em.Exists(immune2),
                "Bomb-immune bullet 2 should survive");
            Assert.IsTrue(_em.Exists(immune3),
                "Bomb-immune bullet 3 should survive");
        }

        [Test]
        public void Bomb_MixedBullets_OnlyImmunesSurvive()
        {
            CreatePlayer(bombStock: 3, bombDuration: 3.0f);
            CreateInputSingleton(bombPressed: true);

            var normal1 = CreateEnemyBullet(new float3(1f, 0f, 0f));
            var immune1 = CreateFlaggedEnemyBullet(new float3(2f, 0f, 0f), bombImmune: true);
            var normal2 = CreateEnemyBullet(new float3(3f, 0f, 0f));
            var flaggedNonImmune = CreateFlaggedEnemyBullet(new float3(4f, 0f, 0f), bombImmune: false);
            var immune2 = CreateFlaggedEnemyBullet(new float3(5f, 0f, 0f), bombImmune: true);

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(normal1),
                "Normal bullet 1 should be destroyed");
            Assert.IsTrue(_em.Exists(immune1),
                "Bomb-immune bullet 1 should survive");
            Assert.IsFalse(_em.Exists(normal2),
                "Normal bullet 2 should be destroyed");
            Assert.IsFalse(_em.Exists(flaggedNonImmune),
                "Flagged non-immune bullet should be destroyed");
            Assert.IsTrue(_em.Exists(immune2),
                "Bomb-immune bullet 2 should survive");
        }
    }
}
