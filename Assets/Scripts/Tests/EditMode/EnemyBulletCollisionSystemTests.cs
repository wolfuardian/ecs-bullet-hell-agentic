using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Player;
using MyGame.ECS.Collision;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// EnemyBulletCollisionSystem EditMode tests.
    /// Validates collision detection for both legacy CollisionRadius bullets
    /// and new BulletHitbox-based danmaku bullets.
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
        /// Create a player entity with standard components.
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
        /// Create a legacy enemy bullet with CollisionRadius.
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
        /// Create a danmaku enemy bullet with BulletHitbox + BulletMotion.
        /// </summary>
        private Entity CreateHitboxBullet(
            float3? pos = null,
            HitboxType hitboxType = HitboxType.Circle,
            float2? hitboxSize = null,
            float2? hitboxOffset = null,
            float angle = -math.PI / 2f, // default: downward
            int damage = 1,
            bool withSpawnDelay = false)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponent<EnemyBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(entity, new BulletHitbox
            {
                Type = hitboxType,
                Size = hitboxSize ?? new float2(0.1f, 0f),
                Offset = hitboxOffset ?? float2.zero
            });
            _em.AddComponentData(entity, new BulletMotion
            {
                Speed = 8f,
                Angle = angle,
                Accel = 0f,
                MaxSpeed = 0f,
                AngularVel = 0f
            });
            _em.AddComponentData(entity, new DamageOnContact { Value = damage });

            if (withSpawnDelay)
            {
                _em.AddComponentData(entity, new SpawnDelay { FramesRemaining = 10 });
            }

            return entity;
        }

        /// <summary>
        /// Advance time and update systems.
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

        // ==========================================
        // Legacy CollisionRadius bullet tests
        // ==========================================

        [Test]
        public void EnemyBullet_DestroysOnPlayerHit()
        {
            CreatePlayer(pos: new float3(0f, 0f, 0f));
            var bullet = CreateEnemyBullet(pos: new float3(0f, 0f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(bullet),
                "Enemy bullet should be destroyed on player hit");
        }

        [Test]
        public void Player_TakesDamage_OnEnemyBulletHit()
        {
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            AdvanceTimeAndUpdate();

            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(2, health.Current,
                "Player HP should be reduced by bullet damage");
        }

        [Test]
        public void Player_DiesWhenHpReachesZero()
        {
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 1);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.HasComponent<DeadTag>(player),
                "Player should have DeadTag when HP reaches zero");
        }

        [Test]
        public void Player_SurvivesWhenHpAboveZero()
        {
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.HasComponent<DeadTag>(player),
                "Player with HP > 0 should not have DeadTag");
        }

        [Test]
        public void Player_InvincibilityActivated_AfterHit()
        {
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3, invDuration: 2.0f);
            CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            AdvanceTimeAndUpdate();

            var timer = _em.GetComponentData<InvincibilityTimer>(player);
            Assert.AreEqual(2.0f, timer.Value, 0.001f,
                "InvincibilityTimer should be set to InvincibilityDuration after hit");
        }

        [Test]
        public void Player_NotHit_WhenInvincible()
        {
            var player = CreatePlayer(pos: new float3(0f, 0f, 0f), hp: 3, invTimer: 1.0f);
            var bullet = CreateEnemyBullet(pos: new float3(0f, 0f, 0f), damage: 1);

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.Exists(bullet),
                "Bullet should survive when player is invincible");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP should be unchanged when invincible");
        }

        [Test]
        public void EnemyBullet_NoCollision_WhenOutOfRange()
        {
            var player = CreatePlayer(pos: new float3(-10f, 0f, 0f), hp: 3);
            var bullet = CreateEnemyBullet(pos: new float3(10f, 0f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.Exists(bullet),
                "Bullet should survive when out of range");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP should be unchanged when no collision");
        }

        [Test]
        public void System_DoesNotRun_WhenNoEnemyBullets()
        {
            CreatePlayer();

            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collisionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            Assert.Pass("System should skip when no EnemyBulletTag entities exist");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPlayer()
        {
            CreateEnemyBullet();

            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _collisionSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            Assert.Pass("System should skip when no PlayerTag entities exist");
        }

        // ==========================================
        // BulletHitbox-based danmaku bullet tests
        // ==========================================

        [Test]
        public void HitboxBullet_Circle_DestroysOnPlayerHit()
        {
            CreatePlayer(pos: float3.zero, radius: 0.08f);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.1f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(bullet),
                "Hitbox bullet (circle) should be destroyed on player hit");
        }

        [Test]
        public void HitboxBullet_Circle_DamagesPlayer()
        {
            var player = CreatePlayer(pos: float3.zero, hp: 3, radius: 0.08f);
            CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.1f, 0f),
                damage: 1);

            AdvanceTimeAndUpdate();

            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(2, health.Current,
                "Player HP should be reduced by hitbox bullet");
        }

        [Test]
        public void HitboxBullet_Circle_MissWhenFar()
        {
            var player = CreatePlayer(pos: new float3(-10f, 0f, 0f), hp: 3);
            var bullet = CreateHitboxBullet(
                pos: new float3(10f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.1f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.Exists(bullet),
                "Hitbox bullet should survive when out of range");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP unchanged when no collision");
        }

        [Test]
        public void HitboxBullet_Rect_DestroysOnPlayerHit()
        {
            CreatePlayer(pos: float3.zero, radius: 0.08f);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Rect,
                hitboxSize: new float2(0.5f, 0.3f));

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(bullet),
                "Hitbox bullet (rect) should be destroyed on player hit");
        }

        [Test]
        public void HitboxBullet_Oval_DestroysOnPlayerHit()
        {
            CreatePlayer(pos: float3.zero, radius: 0.08f);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Oval,
                hitboxSize: new float2(0.5f, 0.3f));

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(bullet),
                "Hitbox bullet (oval) should be destroyed on player hit");
        }

        [Test]
        public void HitboxBullet_Line_DestroysOnPlayerHit()
        {
            CreatePlayer(pos: float3.zero, radius: 0.08f);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Line,
                hitboxSize: new float2(2f, 0.1f),
                angle: 0f);

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(bullet),
                "Hitbox bullet (line) should be destroyed on player hit");
        }

        [Test]
        public void HitboxBullet_None_NeverCollides()
        {
            var player = CreatePlayer(pos: float3.zero, hp: 3);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.None,
                hitboxSize: new float2(100f, 100f));

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.Exists(bullet),
                "Hitbox bullet (none) should never collide");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP unchanged for HitboxType.None");
        }

        [Test]
        public void HitboxBullet_SpawnDelay_SkipsCollision()
        {
            var player = CreatePlayer(pos: float3.zero, hp: 3);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.5f, 0f),
                withSpawnDelay: true);

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.Exists(bullet),
                "Bullet with SpawnDelay should not be destroyed");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP unchanged for bullet with SpawnDelay");
        }

        [Test]
        public void HitboxBullet_WithOffset_CollisionAtOffsetPos()
        {
            // Bullet at origin, hitbox offset to (1, 0), player at (1, 0)
            var player = CreatePlayer(pos: new float3(1f, 0f, 0f), hp: 3, radius: 0.08f);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.1f, 0f),
                hitboxOffset: new float2(1f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsFalse(_em.Exists(bullet),
                "Bullet with offset hitbox should collide at offset position");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(2, health.Current,
                "Player HP reduced from offset hitbox collision");
        }

        [Test]
        public void HitboxBullet_KillsPlayer_WhenHpReachesZero()
        {
            var player = CreatePlayer(pos: float3.zero, hp: 1);
            CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.1f, 0f),
                damage: 1);

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.HasComponent<DeadTag>(player),
                "Player should have DeadTag when HP reaches zero from hitbox bullet");
        }

        [Test]
        public void HitboxBullet_InvincibilityActivated_AfterHit()
        {
            var player = CreatePlayer(pos: float3.zero, hp: 3, invDuration: 2.0f);
            CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.1f, 0f));

            AdvanceTimeAndUpdate();

            var timer = _em.GetComponentData<InvincibilityTimer>(player);
            Assert.AreEqual(2.0f, timer.Value, 0.001f,
                "InvincibilityTimer should be set after hitbox bullet hit");
        }

        [Test]
        public void HitboxBullet_NotHit_WhenPlayerInvincible()
        {
            var player = CreatePlayer(pos: float3.zero, hp: 3, invTimer: 1.0f);
            var bullet = CreateHitboxBullet(
                pos: float3.zero,
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.5f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.Exists(bullet),
                "Hitbox bullet should survive when player is invincible");
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player HP unchanged when invincible");
        }
    }
}
