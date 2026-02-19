using Unity.Entities;

namespace MyGame.ECS.Bomb
{
    /// <summary>
    /// On player entity — bomb stock and cooldown.
    /// Tracks remaining bombs, cooldown timer, and configuration values.
    /// </summary>
    public struct BombData : IComponentData
    {
        /// <summary>Remaining bombs available to the player.</summary>
        public int Stock;

        /// <summary>Current cooldown remaining in seconds.</summary>
        public float CooldownTimer;

        /// <summary>Time between bomb uses in seconds.</summary>
        public float CooldownDuration;

        /// <summary>How long the bomb effect lasts in seconds.</summary>
        public float BombDuration;
    }
}
