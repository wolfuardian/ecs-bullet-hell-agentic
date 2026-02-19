using Unity.Entities;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// On enemy entity — defines what item drops on death.
    /// </summary>
    public struct ItemDropData : IComponentData
    {
        /// <summary>Item type to drop (ItemData constants).</summary>
        public int DropType;

        /// <summary>Drop probability 0.0-1.0.</summary>
        public float DropChance;
    }
}
