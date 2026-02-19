using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;

namespace MyGame.Tests
{
    /// <summary>
    /// BulletMovementSystem + BulletLifetimeSystem 的 EditMode 測試。
    /// 驗證子彈移動、存活時間遞減、超時銷毀。
    /// </summary>
    [TestFixture]
    public class BulletSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _movementSystemHandle;
        private SystemHandle _lifetimeSystemHandle;
        private SystemHandle _ecbSystemHandle;

        /// <summary>測試用固定 DeltaTime（1/60 秒）。</summary>
        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            // BulletLifetimeSystem 需要 EndSimulationEntityCommandBufferSystem
            _ecbSystemHandle = _world.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _movementSystemHandle = _world.GetOrCreateSystem<BulletMovementSystem>();
            _lifetimeSystemHandle = _world.GetOrCreateSystem<BulletLifetimeSystem>();
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
        /// 推進 World 時間並更新指定 System。
        /// </summary>
        private void AdvanceTimeAndUpdate(SystemHandle handle)
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            handle.Update(_world.Unmanaged);
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

        [Test]
        public void BulletMoves_InVelocityDirection()
        {
            // Arrange — 子彈往 +Y 飛
            var bullet = CreateBullet(
                pos: float3.zero,
                velocity: new float3(0f, 20f, 0f));

            // Act
            AdvanceTimeAndUpdate(_movementSystemHandle);

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(bullet).Position;
            Assert.Greater(pos.y, 0f, "Bullet should move in +Y direction");
            Assert.AreEqual(0f, pos.x, 0.001f, "X should not change for vertical bullet");
            Assert.AreEqual(0f, pos.z, 0.001f, "Z should always be 0");
        }

        [Test]
        public void BulletLifetime_DecrementsOverTime()
        {
            // Arrange
            const float initialLifetime = 3f;
            var bullet = CreateBullet(lifetime: initialLifetime);

            // Act
            AdvanceTimeAndUpdate(_lifetimeSystemHandle);

            // Assert
            var remaining = _em.GetComponentData<BulletLifetime>(bullet).Value;
            Assert.Less(remaining, initialLifetime,
                "Bullet lifetime should decrease after system update");
        }

        [Test]
        public void BulletDestroyed_WhenLifetimeExpires()
        {
            // Arrange — 設定極短的存活時間
            var bullet = CreateBullet(lifetime: 0.001f);

            // Act — 跑 lifetime system + ECB playback
            AdvanceTimeAndUpdate(_lifetimeSystemHandle);

            // ECB 在 EndSimulation 時 playback，手動觸發
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert — Entity 應已被銷毀
            Assert.IsFalse(_em.Exists(bullet),
                "Bullet entity should be destroyed when lifetime expires");
        }

        [Test]
        public void BulletNotDestroyed_WhenLifetimeRemaining()
        {
            // Arrange — 存活時間充足
            var bullet = CreateBullet(lifetime: 10f);

            // Act
            AdvanceTimeAndUpdate(_lifetimeSystemHandle);
            _ecbSystemHandle.Update(_world.Unmanaged);

            // Assert — Entity 應仍存在
            Assert.IsTrue(_em.Exists(bullet),
                "Bullet entity should still exist when lifetime > 0");
        }

        [Test]
        public void MultipleBullets_MoveIndependently()
        {
            // Arrange — 兩顆子彈，不同速度
            var bullet1 = CreateBullet(
                pos: float3.zero,
                velocity: new float3(0f, 10f, 0f));
            var bullet2 = CreateBullet(
                pos: float3.zero,
                velocity: new float3(5f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate(_movementSystemHandle);

            // Assert
            var pos1 = _em.GetComponentData<LocalTransform>(bullet1).Position;
            var pos2 = _em.GetComponentData<LocalTransform>(bullet2).Position;

            Assert.Greater(pos1.y, 0f, "Bullet1 should move in +Y");
            Assert.AreEqual(0f, pos1.x, 0.001f, "Bullet1 should not move in X");

            Assert.Greater(pos2.x, 0f, "Bullet2 should move in +X");
            Assert.AreEqual(0f, pos2.y, 0.001f, "Bullet2 should not move in Y");
        }
    }
}
