using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;

namespace MyGame.Tests
{
    /// <summary>
    /// EnemyMovementSystem 的 EditMode 測試。
    /// 驗證敵人移動方向、零速、多敵人獨立移動。
    /// </summary>
    [TestFixture]
    public class EnemyMovementSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _movementSystemHandle;

        /// <summary>測試用固定 DeltaTime（1/60 秒）。</summary>
        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _movementSystemHandle = _world.GetOrCreateSystem<EnemyMovementSystem>();
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
        /// 推進時間並更新 EnemyMovementSystem。
        /// </summary>
        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _movementSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void EnemyMoves_InVelocityDirection()
        {
            // Arrange — 敵人往 +X 方向移動
            var enemy = CreateEnemy(
                pos: float3.zero,
                velocity: new float3(5f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(enemy).Position;
            Assert.Greater(pos.x, 0f, "Enemy should move in +X direction");
            Assert.AreEqual(0f, pos.y, 0.001f, "Y should not change");
        }

        [Test]
        public void EnemyMoves_Downward_WithNegativeYVelocity()
        {
            // Arrange — 典型的敵人：從上往下移動
            var enemy = CreateEnemy(
                pos: new float3(0f, 4f, 0f),
                velocity: new float3(0f, -3f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(enemy).Position;
            Assert.Less(pos.y, 4f, "Enemy should move downward (decreasing Y)");
            Assert.AreEqual(0f, pos.x, 0.001f, "X should not change for vertical enemy");
        }

        [Test]
        public void EnemyDoesNotMove_WhenVelocityIsZero()
        {
            // Arrange — 靜止敵人
            var startPos = new float3(1f, 2f, 0f);
            var enemy = CreateEnemy(
                pos: startPos,
                velocity: float3.zero);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(enemy).Position;
            Assert.AreEqual(startPos.x, pos.x, 0.001f, "X should not change");
            Assert.AreEqual(startPos.y, pos.y, 0.001f, "Y should not change");
        }

        [Test]
        public void MultipleEnemies_MoveIndependently()
        {
            // Arrange — 兩隻敵人，不同速度
            var enemy1 = CreateEnemy(
                pos: float3.zero,
                velocity: new float3(0f, -3f, 0f));
            var enemy2 = CreateEnemy(
                pos: float3.zero,
                velocity: new float3(2f, 0f, 0f));

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var pos1 = _em.GetComponentData<LocalTransform>(enemy1).Position;
            var pos2 = _em.GetComponentData<LocalTransform>(enemy2).Position;

            Assert.Less(pos1.y, 0f, "Enemy1 should move in -Y");
            Assert.AreEqual(0f, pos1.x, 0.001f, "Enemy1 should not move in X");

            Assert.Greater(pos2.x, 0f, "Enemy2 should move in +X");
            Assert.AreEqual(0f, pos2.y, 0.001f, "Enemy2 should not move in Y");
        }

        [Test]
        public void EnemyMovementSystem_DoesNotRun_WhenNoEnemyTag()
        {
            // Arrange — 不建立任何 EnemyTag entity
            // System 的 RequireForUpdate<EnemyTag>() 應讓系統 skip

            // Act — 不應 crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _movementSystemHandle.Update(_world.Unmanaged);

            // Assert — 正常結束即通過
            Assert.Pass("System should skip gracefully when no EnemyTag exists");
        }
    }
}
