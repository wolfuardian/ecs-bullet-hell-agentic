using Unity.Entities;

namespace MyGame.ECS.GameState
{
    /// <summary>
    /// Singleton holding pause/restart input from managed input system.
    /// </summary>
    public struct PauseInputData : IComponentData
    {
        /// <summary>Escape key pressed this frame.</summary>
        public bool PausePressed;

        /// <summary>R key pressed this frame.</summary>
        public bool RestartPressed;
    }
}
