using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;
using MyGame.ECS.Boundary;

namespace MyGame.Tests
{
    /// <summary>
    /// EnemyBoundarySystem 的 EditMode 測試。
    /// 驗證超出邊界的敵人銷毀、邊界上存活、多敵人選擇性銷毀。
    /// </summary>
    [TestFixture]
    public class EnemyBoundarySystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _movementSystemHandle;
        private SystemHandle _boundarySystemHandle;
        private SystemHandle _ecbSystemHandle;

        /// <summary>測試用固定 DeltaTime（1/60 秒）。</summary>
        private const float TEST_DELTA_TIME = 1f / 60f;

        /// <summary>測試用預設邊界（與子彈邊界相同）。</summary>
        private static readonly BulletBoundaryData DEFAULT_BOUNDS = new BulletBoundaryData
        {
            MinX = -4f,
            MaxX = 4f,
            MinY = -5f,
            MaxY = 5f
        };

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _movementSystemHandle = _world.GetOrCreateSystem<EnemyMovementSystem>();
            _boundarySystemHandle = _world.GetOrCreateSystem<EnemyBoundarySystem>();
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
        /// 建立 BulletBoundaryData singleton。
        /// </summary>
        private void CreateBoundary(BulletBoundaryData? bounds = null)
        {
            var boundary = _em.CreateEntity();
            _em.AddComponentData(boundary, bounds ?? DEFAULT_BOUNDS);
        }

        /// <summary>
        /// 建立基本的 Enemy entity。
        /// </summary>
        private Entity CreateEnemy(
            float3? pos = null,
            float3? velocity = null)
        {
            var enemy = _em.CreateEntity();
            _em.AddComponentData(enemy, new EnemyTag());
            _em.AddComponentData(enemy, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(enemy, new EnemyVelocity
            {
                Value = velocity ?? new float3(0f, -3f, 0f)
            });
            return enemy;
        }

        /// <summary>
        /// 推進時間並更新指定 System。
        /// </summary>
        private void AdvanceTimeAndUpdate(SystemHandle handle)
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            handle.Update(_world.Unmanaged);
        }

        [Test]
        public void EnemyDestroyed_WhenPastBottomBound()
        {
            // Arrange
            CreateBoundary();
            var enemy = CreateEnemy(pos: new float3(0f, -6f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(enemy),
                "Enemy past bottom boundary should be destroyed");
        }

        [Test]
        public void EnemyDestroyed_WhenPastLeftBound()
        {
            // Arrange
            CreateBoundary();
            var enemy = CreateEnemy(pos: new float3(-5f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(enemy),
                "Enemy past left boundary should be destroyed");
        }

        [Test]
        public void EnemyDestroyed_WhenPastRightBound()
        {
            // Arrange
            CreateBoundary();
            var enemy = CreateEnemy(pos: new float3(5f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(enemy),
                "Enemy past right boundary should be destroyed");
        }

        [Test]
        public void EnemyDestroyed_WhenPastTopBound()
        {
            // Arrange
            CreateBoundary();
            var enemy = CreateEnemy(pos: new float3(0f, 6f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(enemy),
                "Enemy past top boundary should be destroyed");
        }

        [Test]
        public void EnemyNotDestroyed_WhenInsideBounds()
        {
            // Arrange
            CreateBoundary();
            var enemy = CreateEnemy(pos: new float3(1f, 2f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsTrue(_em.Exists(enemy),
                "Enemy inside bounds should NOT be destroyed");
        }

        [Test]
        public void EnemyNotDestroyed_WhenExactlyOnBound()
        {
            // Arrange — 剛好在邊界上（inclusive check: 不銷毀）
            CreateBoundary();
            var enemy = CreateEnemy(pos: new float3(4f, 0f, 0f)); // MaxX = 4

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsTrue(_em.Exists(enemy),
                "Enemy exactly on boundary edge should NOT be destroyed");
        }

        [Test]
        public void EnemyBoundarySystem_DoesNotRun_WhenNoBoundarySingleton()
        {
            // Arrange — 不建立 Boundary singleton
            var enemy = CreateEnemy(pos: new float3(100f, 0f, 0f));

            // Act — 不應 crash（RequireForUpdate 會讓系統 skip）
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _boundarySystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert — 敵人仍然存在
            Assert.IsTrue(_em.Exists(enemy),
                "Enemy should survive when no boundary singleton exists");
        }

        [Test]
        public void MultipleEnemies_OnlyOutOfBoundsDestroyed()
        {
            // Arrange — 3 隻敵人：中心、左邊界外、下邊界外
            CreateBoundary();
            var inside = CreateEnemy(pos: new float3(0f, 0f, 0f));
            var pastLeft = CreateEnemy(pos: new float3(-5f, 0f, 0f));
            var pastBottom = CreateEnemy(pos: new float3(0f, -6f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsTrue(_em.Exists(inside),
                "In-bounds enemy should survive");
            Assert.IsFalse(_em.Exists(pastLeft),
                "Left OOB enemy should be destroyed");
            Assert.IsFalse(_em.Exists(pastBottom),
                "Bottom OOB enemy should be destroyed");
        }
    }
}
