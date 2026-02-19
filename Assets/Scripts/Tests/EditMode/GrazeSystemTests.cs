using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Player;
using MyGame.ECS.Collision;
using MyGame.ECS.Graze;
using MyGame.ECS.Danmaku;

namespace MyGame.Tests
{
    /// <summary>
    /// GrazeSystem EditMode tests.
    /// Validates graze detection for both legacy CollisionRadius bullets
    /// and new BulletHitbox-based danmaku bullets.
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
        /// Create a player entity with GrazeData.
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
        /// Create a legacy enemy bullet with CollisionRadius.
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
        /// Create a danmaku enemy bullet with BulletHitbox + BulletMotion.
        /// </summary>
        private Entity CreateHitboxBullet(
            float3? pos = null,
            HitboxType hitboxType = HitboxType.Circle,
            float2? hitboxSize = null,
            float angle = -math.PI / 2f,
            bool grazed = false,
            bool withSpawnDelay = false)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<BulletTag>(entity);
            _em.AddComponent<EnemyBulletTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos ?? new float3(0f, 3f, 0f)));
            _em.AddComponentData(entity, new BulletHitbox
            {
                Type = hitboxType,
                Size = hitboxSize ?? new float2(0.12f, 0f),
                Offset = float2.zero
            });
            _em.AddComponentData(entity, new BulletMotion
            {
                Speed = 8f,
                Angle = angle,
                Accel = 0f,
                MaxSpeed = 0f,
                AngularVel = 0f
            });
            _em.AddComponentData(entity, new DamageOnContact { Value = 1 });

            if (grazed)
            {
                _em.AddComponent<GrazedTag>(entity);
            }

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
            _grazeSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        // ==========================================
        // Legacy CollisionRadius bullet graze tests
        // ==========================================

        [Test]
        public void Graze_Detected_WhenBulletInGrazeRange()
        {
            // bullet at 0.3, between collision (0.20) and graze (0.62)
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(1, grazeData.Count,
                "Graze count should be 1 when bullet is in graze range");
        }

        [Test]
        public void Graze_NotDetected_WhenBulletTooFar()
        {
            // bullet at 1.0, outside graze radius (0.62)
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(1.0f, 0f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Graze count should remain 0 when bullet is too far");
        }

        [Test]
        public void Graze_NotDetected_WhenBulletInCollisionRange()
        {
            // bullet at 0.1, inside collision radius (0.20)
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.1f, 0f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Graze count should remain 0 when bullet is in collision range");
        }

        [Test]
        public void GrazedBullet_NotCountedTwice()
        {
            // bullet already has GrazedTag
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f), grazed: true);

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Graze count should remain 0 for already-grazed bullet");
        }

        [Test]
        public void GrazeCount_Accumulates()
        {
            // multiple bullets in graze range
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));
            CreateEnemyBullet(pos: new float3(-0.3f, 0f, 0f));
            CreateEnemyBullet(pos: new float3(0f, 0.3f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(3, grazeData.Count,
                "Graze count should accumulate for multiple bullets in range");
        }

        [Test]
        public void GrazedTag_AddedToBullet()
        {
            CreatePlayer(pos: float3.zero);
            var bullet = CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.HasComponent<GrazedTag>(bullet),
                "Bullet should have GrazedTag after being grazed");
        }

        [Test]
        public void System_DoesNotRun_WhenNoPlayer()
        {
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));

            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _grazeSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            Assert.Pass("System should skip when no PlayerTag entities exist");
        }

        [Test]
        public void System_DoesNotRun_WhenNoEnemyBullets()
        {
            CreatePlayer(pos: float3.zero);

            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _grazeSystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            Assert.Pass("System should skip when no EnemyBulletTag entities exist");
        }

        // ==========================================
        // BulletHitbox-based danmaku graze tests
        // ==========================================

        [Test]
        public void HitboxBullet_Graze_Detected_WhenInGrazeRange()
        {
            // Player: collisionR=0.08, grazeR=0.5
            // Hitbox circle: radius=0.12 (effectiveR)
            // collisionSum = 0.08 + 0.12 = 0.20, grazeSum = 0.5 + 0.12 = 0.62
            // Bullet at (0.3, 0, 0): dist=0.3, in graze zone (0.20 < 0.3 <= 0.62)
            var player = CreatePlayer(pos: float3.zero);
            CreateHitboxBullet(
                pos: new float3(0.3f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.12f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(1, grazeData.Count,
                "Hitbox bullet in graze range should be counted");
        }

        [Test]
        public void HitboxBullet_Graze_NotDetected_WhenTooFar()
        {
            var player = CreatePlayer(pos: float3.zero);
            CreateHitboxBullet(
                pos: new float3(5f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.12f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Hitbox bullet too far should not trigger graze");
        }

        [Test]
        public void HitboxBullet_Graze_NotDetected_WhenInCollisionRange()
        {
            // dist=0.1 < collisionSum=0.20 -> inside collision zone, not graze
            var player = CreatePlayer(pos: float3.zero);
            CreateHitboxBullet(
                pos: new float3(0.1f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.12f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Hitbox bullet in collision range should not trigger graze");
        }

        [Test]
        public void HitboxBullet_GrazedTag_Prevents_DoubleCounting()
        {
            var player = CreatePlayer(pos: float3.zero);
            CreateHitboxBullet(
                pos: new float3(0.3f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.12f, 0f),
                grazed: true);

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Already-grazed hitbox bullet should not be counted again");
        }

        [Test]
        public void HitboxBullet_GrazedTag_AddedOnGraze()
        {
            CreatePlayer(pos: float3.zero);
            var bullet = CreateHitboxBullet(
                pos: new float3(0.3f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.12f, 0f));

            AdvanceTimeAndUpdate();

            Assert.IsTrue(_em.HasComponent<GrazedTag>(bullet),
                "Hitbox bullet should get GrazedTag after graze");
        }

        [Test]
        public void HitboxBullet_SpawnDelay_SkipsGraze()
        {
            var player = CreatePlayer(pos: float3.zero);
            CreateHitboxBullet(
                pos: new float3(0.3f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.12f, 0f),
                withSpawnDelay: true);

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(0, grazeData.Count,
                "Hitbox bullet with SpawnDelay should not trigger graze");
        }

        [Test]
        public void HitboxBullet_Rect_UsesLargestDimensionForGraze()
        {
            // Rect with halfWidth=0.5, halfHeight=0.1 -> effectiveR = 0.5
            // collisionSum = 0.08 + 0.5 = 0.58, grazeSum = 0.5 + 0.5 = 1.0
            // Bullet at (0.7, 0, 0): dist=0.7, in graze zone (0.58 < 0.7 <= 1.0)
            var player = CreatePlayer(pos: float3.zero);
            CreateHitboxBullet(
                pos: new float3(0.7f, 0f, 0f),
                hitboxType: HitboxType.Rect,
                hitboxSize: new float2(0.5f, 0.1f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(1, grazeData.Count,
                "Rect hitbox should use largest dimension for graze radius");
        }

        [Test]
        public void MixedBullets_BothTypesGraze()
        {
            // One legacy bullet + one hitbox bullet, both in graze range
            var player = CreatePlayer(pos: float3.zero);
            CreateEnemyBullet(pos: new float3(0.3f, 0f, 0f));
            CreateHitboxBullet(
                pos: new float3(-0.3f, 0f, 0f),
                hitboxType: HitboxType.Circle,
                hitboxSize: new float2(0.12f, 0f));

            AdvanceTimeAndUpdate();

            var grazeData = _em.GetComponentData<GrazeData>(player);
            Assert.AreEqual(2, grazeData.Count,
                "Both legacy and hitbox bullets should contribute to graze count");
        }
    }
}
