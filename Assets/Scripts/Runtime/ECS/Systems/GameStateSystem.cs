using Unity.Burst;
using Unity.Entities;
using MyGame.ECS.Player;

namespace MyGame.ECS.GameState
{
    /// <summary>
    /// Manages game state transitions: playing, paused, and game over.
    /// Detects player death (no PlayerTag entity) and handles pause/restart input.
    /// Runs after DeathSystem so destroyed entities are already removed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Collision.DeathSystem))]
    public partial struct GameStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameStateData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingletonRW<GameStateData>();
            int current = gameState.ValueRO.State;

            // Read pause/restart input if available
            bool pausePressed = false;
            bool restartPressed = false;
            if (SystemAPI.HasSingleton<PauseInputData>())
            {
                var input = SystemAPI.GetSingleton<PauseInputData>();
                pausePressed = input.PausePressed;
                restartPressed = input.RestartPressed;
            }

            // Game Over detection: if playing and no player exists
            if (current == GameStateData.PLAYING)
            {
                var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerTag>().Build();
                if (playerQuery.IsEmpty)
                {
                    gameState.ValueRW.State = GameStateData.GAME_OVER;
                    return;
                }
            }

            // Pause toggle
            if (pausePressed)
            {
                if (current == GameStateData.PLAYING)
                {
                    gameState.ValueRW.State = GameStateData.PAUSED;
                    return;
                }
                if (current == GameStateData.PAUSED)
                {
                    gameState.ValueRW.State = GameStateData.PLAYING;
                    return;
                }
            }

            // Restart from game over
            if (restartPressed && current == GameStateData.GAME_OVER)
            {
                gameState.ValueRW.State = GameStateData.PLAYING;
            }
        }
    }
}
