using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.GameState;
using MyGame.ECS.Player;

namespace MyGame.Tests
{
    /// <summary>
    /// GameStateSystem 的 EditMode 測試。
    /// 驗證 Game Over 偵測、暫停切換、重新開始邏輯。
    /// </summary>
    [TestFixture]
    public class GameStateSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _gameStateSystemHandle;

        private const float TEST_DELTA_TIME = 1f / 60f;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _gameStateSystemHandle = _world.GetOrCreateSystem<GameStateSystem>();
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
        /// 建立 GameStateData singleton entity。
        /// </summary>
        private Entity CreateGameStateSingleton(int state = GameStateData.PLAYING)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new GameStateData { State = state });
            return entity;
        }

        /// <summary>
        /// 建立 PauseInputData singleton entity。
        /// </summary>
        private Entity CreatePauseInputSingleton(bool pause = false, bool restart = false)
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new PauseInputData
            {
                PausePressed = pause,
                RestartPressed = restart
            });
            return entity;
        }

        /// <summary>
        /// 建立帶 PlayerTag 和 LocalTransform 的玩家 entity。
        /// </summary>
        private Entity CreatePlayer()
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlayerTag>(entity);
            _em.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
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
            _gameStateSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void GameOver_WhenNoPlayerExists()
        {
            // Arrange — state is PLAYING, no player entity
            CreateGameStateSingleton(GameStateData.PLAYING);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(GameStateData));
            var gameState = query.GetSingleton<GameStateData>();
            Assert.AreEqual(GameStateData.GAME_OVER, gameState.State,
                "State should transition to GAME_OVER when no player exists");
        }

        [Test]
        public void State_RemainsPlaying_WhenPlayerAlive()
        {
            // Arrange — state is PLAYING, player exists
            CreateGameStateSingleton(GameStateData.PLAYING);
            CreatePlayer();

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(GameStateData));
            var gameState = query.GetSingleton<GameStateData>();
            Assert.AreEqual(GameStateData.PLAYING, gameState.State,
                "State should remain PLAYING when player is alive");
        }

        [Test]
        public void Pause_TogglesPlayingToPaused()
        {
            // Arrange — state is PLAYING, player exists, pause pressed
            CreateGameStateSingleton(GameStateData.PLAYING);
            CreatePlayer();
            CreatePauseInputSingleton(pause: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(GameStateData));
            var gameState = query.GetSingleton<GameStateData>();
            Assert.AreEqual(GameStateData.PAUSED, gameState.State,
                "State should toggle from PLAYING to PAUSED when pause pressed");
        }

        [Test]
        public void Pause_TogglesPausedToPlaying()
        {
            // Arrange — state is PAUSED, pause pressed
            CreateGameStateSingleton(GameStateData.PAUSED);
            CreatePauseInputSingleton(pause: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(GameStateData));
            var gameState = query.GetSingleton<GameStateData>();
            Assert.AreEqual(GameStateData.PLAYING, gameState.State,
                "State should toggle from PAUSED to PLAYING when pause pressed");
        }

        [Test]
        public void Pause_IgnoredDuringGameOver()
        {
            // Arrange — state is GAME_OVER, pause pressed
            CreateGameStateSingleton(GameStateData.GAME_OVER);
            CreatePauseInputSingleton(pause: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(GameStateData));
            var gameState = query.GetSingleton<GameStateData>();
            Assert.AreEqual(GameStateData.GAME_OVER, gameState.State,
                "Pause should be ignored during GAME_OVER state");
        }

        [Test]
        public void Restart_ResetsGameOverToPlaying()
        {
            // Arrange — state is GAME_OVER, restart pressed
            CreateGameStateSingleton(GameStateData.GAME_OVER);
            CreatePauseInputSingleton(restart: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(GameStateData));
            var gameState = query.GetSingleton<GameStateData>();
            Assert.AreEqual(GameStateData.PLAYING, gameState.State,
                "Restart should reset GAME_OVER to PLAYING");
        }

        [Test]
        public void Restart_IgnoredDuringPlaying()
        {
            // Arrange — state is PLAYING, player exists, restart pressed
            CreateGameStateSingleton(GameStateData.PLAYING);
            CreatePlayer();
            CreatePauseInputSingleton(restart: true);

            // Act
            AdvanceTimeAndUpdate();

            // Assert
            var query = _em.CreateEntityQuery(typeof(GameStateData));
            var gameState = query.GetSingleton<GameStateData>();
            Assert.AreEqual(GameStateData.PLAYING, gameState.State,
                "Restart should be ignored during PLAYING state");
        }

        [Test]
        public void System_DoesNotRun_WhenNoGameStateData()
        {
            // Arrange — no GameStateData singleton

            // Act — should not crash
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _gameStateSystemHandle.Update(_world.Unmanaged);

            // Assert
            Assert.Pass("System should skip when no GameStateData exists");
        }
    }
}
