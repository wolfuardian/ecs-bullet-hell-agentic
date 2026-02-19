using Unity.Entities;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Item type and effect values.
    /// </summary>
    public struct ItemData : IComponentData
    {
        /// <summary>Score item type constant.</summary>
        public const int SCORE_ITEM = 0;

        /// <summary>Power item type constant.</summary>
        public const int POWER_ITEM = 1;

        /// <summary>Bomb item type constant.</summary>
        public const int BOMB_ITEM = 2;

        /// <summary>Item type (use constants above).</summary>
        public int Type;

        /// <summary>Score value for SCORE_ITEM.</summary>
        public int ScoreValue;

        /// <summary>Power increment for POWER_ITEM.</summary>
        public int PowerValue;
    }
}
