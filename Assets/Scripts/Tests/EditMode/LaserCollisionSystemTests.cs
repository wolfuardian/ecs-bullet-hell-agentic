using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;
using MyGame.ECS.Danmaku;
using MyGame.ECS.Player;

namespace MyGame.Tests
{
    /// <summary>
    /// EditMode tests for LaserCollisionSystem.
    /// Validates beam and curve laser collision with player, invincibility, and damage.
    /// </summary>
    [TestFixture]
    public class LaserCollisionSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _systemHandle;
        private SystemHandle _ecbSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;
            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _systemHandle = _world.GetOrCreateSystem<LaserCollisionSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _systemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);
        }

        /// <summary>
        /// Creates a player entity with all required components.
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
        /// Creates a beam laser entity (already active, with positive length).
        /// </summary>
        private Entity CreateBeamLaser(
            float3? origin = null,
            float angle = 0f,
            float length = 10f,
            float maxLength = 10f,
            float width = 1f,
            bool active = true,
            float duration = 5f,
            int damage = 1)
        {
            var pos = origin ?? float3.zero;
            var entity = _em.CreateEntity();
            _em.AddComponent<LaserTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(pos));
            _em.AddComponentData(entity, new LaserBeam
            {
                Origin = pos,
                Angle = angle,
                Length = length,
                MaxLength = maxLength,
                Width = width,
                Color = BulletColor.Red,
                Active = active,
                WarningTimer = 0f,
                Duration = duration,
            });
            _em.AddComponentData(entity, new DamageOnContact { Value = damage });
            return entity;
        }

        /// <summary>
        /// Creates a curve laser entity with multiple points.
        /// </summary>
        private Entity CreateCurveLaserWithPoints(
            float3[] points,
            float width = 1f,
            int damage = 1)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<LaserTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(points[0]));
            _em.AddComponentData(entity, new CurveLaser
            {
                Width = width,
                Color = BulletColor.Blue,
                SegmentCount = points.Length,
                Duration = 5f,
            });
            _em.AddComponentData(entity, new DamageOnContact { Value = damage });

            var buffer = _em.AddBuffer<CurveLaserPoint>(entity);
            for (int i = 0; i < points.Length; i++)
            {
                buffer.Add(new CurveLaserPoint
                {
                    Position = points[i],
                    Velocity = float3.zero,
                });
            }

            return entity;
        }

        [Test]
        public void BeamHitsPlayerInLine()
        {
            // Arrange — beam goes right from origin, player is at (3,0)
            CreatePlayer(pos: new float3(3f, 0f, 0f), radius: 0.1f);
            CreateBeamLaser(
                origin: float3.zero,
                angle: 0f, // points right (+X)
                length: 10f,
                width: 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — player should have taken damage
            var player = GetSinglePlayerEntity();
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(2, health.Current,
                "Player on beam path should take damage");
        }

        [Test]
        public void BeamMissesPlayerOutsideWidth()
        {
            // Arrange — beam goes right, player is far above the beam
            CreatePlayer(pos: new float3(3f, 5f, 0f), radius: 0.1f);
            CreateBeamLaser(
                origin: float3.zero,
                angle: 0f,
                length: 10f,
                width: 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — player should not be hit
            var player = GetSinglePlayerEntity();
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Player outside beam width should not take damage");
        }

        [Test]
        public void InactiveBeamDoesNotHit()
        {
            // Arrange — beam is in warning phase (not active)
            CreatePlayer(pos: new float3(3f, 0f, 0f), radius: 0.1f);
            CreateBeamLaser(
                origin: float3.zero,
                angle: 0f,
                length: 10f,
                width: 1f,
                active: false);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var player = GetSinglePlayerEntity();
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Inactive beam should not damage player");
        }

        [Test]
        public void CurveLaserHitsPlayer()
        {
            // Arrange — curve laser passes through player at (2,0)
            CreatePlayer(pos: new float3(2f, 0f, 0f), radius: 0.1f);
            CreateCurveLaserWithPoints(
                new float3[]
                {
                    new float3(0f, 0f, 0f),
                    new float3(1f, 0f, 0f),
                    new float3(2f, 0f, 0f),
                    new float3(3f, 0f, 0f),
                },
                width: 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var player = GetSinglePlayerEntity();
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(2, health.Current,
                "Player on curve laser path should take damage");
        }

        [Test]
        public void PlayerInvincible_NoHit()
        {
            // Arrange — player is invincible
            CreatePlayer(
                pos: new float3(3f, 0f, 0f),
                radius: 0.1f,
                hp: 3,
                invTimer: 1.0f);
            CreateBeamLaser(
                origin: float3.zero,
                angle: 0f,
                length: 10f,
                width: 1f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var player = GetSinglePlayerEntity();
            var health = _em.GetComponentData<HealthData>(player);
            Assert.AreEqual(3, health.Current,
                "Invincible player should not take laser damage");
        }

        [Test]
        public void BeamHit_GrantsInvincibility()
        {
            // Arrange
            CreatePlayer(
                pos: new float3(3f, 0f, 0f),
                radius: 0.1f,
                hp: 3,
                invDuration: 2.0f);
            CreateBeamLaser(
                origin: float3.zero,
                angle: 0f,
                length: 10f,
                width: 1f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var player = GetSinglePlayerEntity();
            var invTimer = _em.GetComponentData<InvincibilityTimer>(player);
            Assert.AreEqual(2.0f, invTimer.Value, 0.001f,
                "Invincibility timer should be set after laser hit");
        }

        [Test]
        public void BeamHit_KillsPlayerAtOneHp()
        {
            // Arrange — player with 1 HP
            var player = CreatePlayer(
                pos: new float3(3f, 0f, 0f),
                radius: 0.1f,
                hp: 1);
            CreateBeamLaser(
                origin: float3.zero,
                angle: 0f,
                length: 10f,
                width: 1f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            Assert.IsTrue(_em.HasComponent<DeadTag>(player),
                "Player should have DeadTag when HP reaches zero from laser");
        }

        /// <summary>
        /// Helper to find the single player entity.
        /// </summary>
        private Entity GetSinglePlayerEntity()
        {
            var query = _em.CreateEntityQuery(typeof(PlayerTag));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var result = entities.Length > 0 ? entities[0] : Entity.Null;
            entities.Dispose();
            return result;
        }
    }
}
