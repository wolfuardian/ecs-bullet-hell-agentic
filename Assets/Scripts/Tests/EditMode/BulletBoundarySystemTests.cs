using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Boundary;

namespace MyGame.Tests
{
    /// <summary>
    /// BulletBoundarySystem 的 EditMode 測試。
    /// 驗證超出邊界的子彈銷毀、邊界上存活、多子彈選擇性銷毀。
    /// </summary>
    [TestFixture]
    public class BulletBoundarySystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _movementSystemHandle;
        private SystemHandle _boundarySystemHandle;
        private SystemHandle _ecbSystemHandle;

        /// <summary>測試用固定 DeltaTime（1/60 秒）。</summary>
        private const float TEST_DELTA_TIME = 1f / 60f;

        /// <summary>測試用預設子彈邊界（比玩家邊界大 2.0 margin）。</summary>
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
            _movementSystemHandle = _world.GetOrCreateSystem<BulletMovementSystem>();
            _boundarySystemHandle = _world.GetOrCreateSystem<BulletBoundarySystem>();
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
        /// 建立基本的 Bullet entity。
        /// </summary>
        private Entity CreateBullet(
            float3? pos = null,
            float3? velocity = null,
            float lifetime = 3f)
        {
            var bullet = _em.CreateEntity();
            _em.AddComponentData(bullet, new BulletTag());
            _em.AddComponentData(bullet, LocalTransform.FromPosition(pos ?? float3.zero));
            _em.AddComponentData(bullet, new Velocity { Value = velocity ?? new float3(0f, 20f, 0f) });
            _em.AddComponentData(bullet, new BulletLifetime { Value = lifetime });
            return bullet;
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
        public void BulletDestroyed_WhenPastRightBound()
        {
            // Arrange
            CreateBoundary();
            var bullet = CreateBullet(pos: new float3(5f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Bullet past right boundary should be destroyed");
        }

        [Test]
        public void BulletDestroyed_WhenPastLeftBound()
        {
            // Arrange
            CreateBoundary();
            var bullet = CreateBullet(pos: new float3(-5f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Bullet past left boundary should be destroyed");
        }

        [Test]
        public void BulletDestroyed_WhenPastTopBound()
        {
            // Arrange
            CreateBoundary();
            var bullet = CreateBullet(pos: new float3(0f, 6f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Bullet past top boundary should be destroyed");
        }

        [Test]
        public void BulletDestroyed_WhenPastBottomBound()
        {
            // Arrange
            CreateBoundary();
            var bullet = CreateBullet(pos: new float3(0f, -6f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Bullet past bottom boundary should be destroyed");
        }

        [Test]
        public void BulletNotDestroyed_WhenInsideBounds()
        {
            // Arrange
            CreateBoundary();
            var bullet = CreateBullet(pos: new float3(1f, 2f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet inside bounds should NOT be destroyed");
        }

        [Test]
        public void BulletNotDestroyed_WhenExactlyOnBound()
        {
            // Arrange — 剛好在邊界上（inclusive check: 不銷毀）
            CreateBoundary();
            var bullet = CreateBullet(pos: new float3(4f, 0f, 0f)); // MaxX = 4

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet exactly on boundary edge should NOT be destroyed");
        }

        [Test]
        public void BulletDestroyed_AfterMovementPushesPastBound()
        {
            // Arrange — 子彈在邊界內，但高速往 +Y 飛
            CreateBoundary(); // MaxY = 5
            var bullet = CreateBullet(
                pos: new float3(0f, 4.9f, 0f),
                velocity: new float3(0f, 100f, 0f)); // 極高速，一幀就超出

            // Act — 先移動再檢查邊界
            AdvanceTimeAndUpdate(_movementSystemHandle);
            _boundarySystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Bullet should be destroyed after movement pushes it past boundary");
        }

        [Test]
        public void MultipleBullets_OnlyOutOfBoundsDestroyed()
        {
            // Arrange — 3 顆子彈：中心、左邊界外、上邊界外
            CreateBoundary();
            var inside = CreateBullet(pos: new float3(0f, 0f, 0f));
            var pastLeft = CreateBullet(pos: new float3(-5f, 0f, 0f));
            var pastTop = CreateBullet(pos: new float3(0f, 6f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsTrue(_em.Exists(inside),
                "In-bounds bullet should survive");
            Assert.IsFalse(_em.Exists(pastLeft),
                "Left OOB bullet should be destroyed");
            Assert.IsFalse(_em.Exists(pastTop),
                "Top OOB bullet should be destroyed");
        }

        [Test]
        public void BulletBoundarySystem_DoesNotRun_WhenNoBoundarySingleton()
        {
            // Arrange — 不建立 Boundary singleton
            var bullet = CreateBullet(pos: new float3(100f, 0f, 0f));

            // Act — 不應 crash（RequireForUpdate 會讓系統 skip）
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _boundarySystemHandle.Update(_world.Unmanaged);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert — 子彈仍然存在
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet should survive when no boundary singleton exists");
        }

        [Test]
        public void BulletDestroyed_WithCustomBoundaryValues()
        {
            // Arrange — 使用小範圍自訂邊界
            var customBounds = new BulletBoundaryData
            {
                MinX = -1f,
                MaxX = 1f,
                MinY = -1f,
                MaxY = 1f
            };
            CreateBoundary(customBounds);

            // 這個位置在預設邊界內但在自訂邊界外
            var bullet = CreateBullet(pos: new float3(2f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate(_boundarySystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.IsFalse(_em.Exists(bullet),
                "Bullet should be destroyed based on custom boundary (MaxX=1), not default");
        }
    }
}
