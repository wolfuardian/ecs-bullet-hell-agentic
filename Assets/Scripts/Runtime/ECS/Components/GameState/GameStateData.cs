using Unity.Entities;

namespace MyGame.ECS.GameState
{
    /// <summary>
    /// Singleton holding game state. Uses int constants for Burst compatibility.
    /// </summary>
    public struct GameStateData : IComponentData
    {
        /// <summary>Game is actively running.</summary>
        public const int PLAYING = 0;

        /// <summary>Game is paused.</summary>
        public const int PAUSED = 1;

        /// <summary>Player has been destroyed; game is over.</summary>
        public const int GAME_OVER = 2;

        /// <summary>Current game state.</summary>
        public int State;
    }
}
