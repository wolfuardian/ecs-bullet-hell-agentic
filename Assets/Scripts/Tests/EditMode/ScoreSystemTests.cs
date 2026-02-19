using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;
using MyGame.ECS.Score;

namespace MyGame.Tests
{
    /// <summary>
    /// ScoreSystem 的 EditMode 測試。
    /// 驗證擊殺敵人時分數正確累加。
    /// </summary>
    [TestFixture]
    public class ScoreSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _scoreSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _scoreSystemHandle = _world.GetOrCreateSystem<ScoreSystem>();
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
        /// 推進時間並更新 ScoreSystem。
        /// </summary>
        private void AdvanceTimeAndUpdate()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _scoreSystemHandle.Update(_world.Unmanaged);
        }

        /// <summary>
        /// 建立 ScoreData singleton entity。
        /// </summary>
        private Entity CreateScoreSingleton(int initialScore = 0)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new ScoreData { Value = initialScore });
            return entity;
        }

        /// <summary>
        /// 建立帶有 ScoreOnDeath 的敵人 entity，可選擇是否標記為死亡。
        /// </summary>
        private Entity CreateEnemyWithScore(float3? pos, int scoreValue, bool isDead)
        {
            var entity = _em.CreateEntity();
            var position = pos ?? float3.zero;
            _em.AddComponentData(entity, LocalTransform.FromPosition(position));
            _em.AddComponentData(entity, new ScoreOnDeath { Value = scoreValue });

            if (isDead)
            {
                _em.AddComponent<DeadTag>(entity);
            }

            return entity;
        }

        [Test]
        public void Score_Increases_WhenEnemyWithDeadTagExists()
        {
            // Arrange
            CreateScoreSingleton(0);
            CreateEnemyWithScore(float3.zero, 100, isDead: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var scoreEntity = _em.CreateEntityQuery(typeof(ScoreData))
                .GetSingletonEntity();
            var score = _em.GetComponentData<ScoreData>(scoreEntity);
            Assert.AreEqual(100, score.Value,
                "Score should increase by enemy's ScoreOnDeath value");
        }

        [Test]
        public void Score_DoesNotChange_WhenEnemyAliveWithScoreOnDeath()
        {
            // Arrange
            CreateScoreSingleton(50);
            CreateEnemyWithScore(float3.zero, 200, isDead: false);

            // 需要至少一個 DeadTag entity 才能讓系統執行
            // 但這個測試驗證的是沒有 DeadTag 時系統不跑，所以不需要
            // RequireForUpdate<DeadTag> 會讓系統跳過

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var scoreEntity = _em.CreateEntityQuery(typeof(ScoreData))
                .GetSingletonEntity();
            var score = _em.GetComponentData<ScoreData>(scoreEntity);
            Assert.AreEqual(50, score.Value,
                "Score should not change when no dead enemies exist");
        }

        [Test]
        public void Score_AccumulatesFromMultipleDeadEnemies()
        {
            // Arrange
            CreateScoreSingleton(0);
            CreateEnemyWithScore(float3.zero, 100, isDead: true);
            CreateEnemyWithScore(new float3(1, 0, 0), 200, isDead: true);
            CreateEnemyWithScore(new float3(2, 0, 0), 50, isDead: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var scoreEntity = _em.CreateEntityQuery(typeof(ScoreData))
                .GetSingletonEntity();
            var score = _em.GetComponentData<ScoreData>(scoreEntity);
            Assert.AreEqual(350, score.Value,
                "Score should accumulate points from all dead enemies");
        }

        [Test]
        public void Score_DoesNotChange_WhenDeadEntityHasNoScoreOnDeath()
        {
            // Arrange
            CreateScoreSingleton(25);

            // 建立有 DeadTag 但沒有 ScoreOnDeath 的 entity
            var deadEntity = _em.CreateEntity();
            _em.AddComponent<DeadTag>(deadEntity);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var scoreEntity = _em.CreateEntityQuery(typeof(ScoreData))
                .GetSingletonEntity();
            var score = _em.GetComponentData<ScoreData>(scoreEntity);
            Assert.AreEqual(25, score.Value,
                "Score should not change when dead entity has no ScoreOnDeath");
        }

        [Test]
        public void System_DoesNotRun_WhenNoScoreDataSingleton()
        {
            // Arrange — 不建立 ScoreData singleton
            CreateEnemyWithScore(float3.zero, 100, isDead: true);

            // Act — 不應 crash（RequireForUpdate<ScoreData> 阻止執行）
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _scoreSystemHandle.Update(_world.Unmanaged);

            // Assert — 只要不 crash 即通過
            Assert.Pass("System should not run without ScoreData singleton");
        }

        [Test]
        public void System_DoesNotRun_WhenNoDeadTagEntities()
        {
            // Arrange — 有 ScoreData 但沒有 DeadTag entity
            CreateScoreSingleton(10);

            // Act — 不應 crash（RequireForUpdate<DeadTag> 阻止執行）
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _scoreSystemHandle.Update(_world.Unmanaged);

            // Assert
            var scoreEntity = _em.CreateEntityQuery(typeof(ScoreData))
                .GetSingletonEntity();
            var score = _em.GetComponentData<ScoreData>(scoreEntity);
            Assert.AreEqual(10, score.Value,
                "Score should remain unchanged when no DeadTag entities exist");
        }
    }
}
