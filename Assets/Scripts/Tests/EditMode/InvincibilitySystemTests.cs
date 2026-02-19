using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using MyGame.ECS.Collision;

namespace MyGame.Tests
{
    /// <summary>
    /// InvincibilitySystem 的 EditMode 測試。
    /// 驗證無敵計時器遞減、歸零夾持、零值不變。
    /// </summary>
    [TestFixture]
    public class InvincibilitySystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _invincibilitySystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _invincibilitySystemHandle = _world.GetOrCreateSystem<InvincibilitySystem>();
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
        /// 建立帶有 InvincibilityTimer 的 entity。
        /// </summary>
        private Entity CreateEntityWithTimer(float timerValue)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new InvincibilityTimer { Value = timerValue });
            return entity;
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
            _invincibilitySystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void InvincibilityTimer_DecrementsOverTime()
        {
            // Arrange
            var entity = CreateEntityWithTimer(1.0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var timer = _em.GetComponentData<InvincibilityTimer>(entity);
            Assert.Less(timer.Value, 1.0f,
                "InvincibilityTimer should decrease after one frame");
            Assert.AreEqual(1.0f - TEST_DELTA_TIME, timer.Value, 0.001f,
                "Timer should decrease by exactly deltaTime");
        }

        [Test]
        public void InvincibilityTimer_ClampsToZero()
        {
            // Arrange — timer 即將歸零（小於一幀的量）
            var entity = CreateEntityWithTimer(TEST_DELTA_TIME * 0.5f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 不應變成負數
            var timer = _em.GetComponentData<InvincibilityTimer>(entity);
            Assert.AreEqual(0f, timer.Value, 0.001f,
                "InvincibilityTimer should clamp to zero, not go negative");
        }

        [Test]
        public void InvincibilityTimer_NoChange_WhenAlreadyZero()
        {
            // Arrange — timer 已經是零
            var entity = CreateEntityWithTimer(0f);

            // Act
            AdvanceTimeAndUpdate();

            // Assert — 維持零
            var timer = _em.GetComponentData<InvincibilityTimer>(entity);
            Assert.AreEqual(0f, timer.Value, 0.001f,
                "InvincibilityTimer at zero should remain zero");
        }

        [Test]
        public void System_DoesNotRun_WhenNoTimerExists()
        {
            // Arrange — 不建立任何有 InvincibilityTimer 的 entity

            // Act — 不應 crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _invincibilitySystemHandle.Update(_world.Unmanaged);

            // Assert — 正常結束即通過
            Assert.Pass("System should skip gracefully when no InvincibilityTimer exists");
        }
    }
}
