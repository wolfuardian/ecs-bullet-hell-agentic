using Unity.Entities;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// On player entity — current power level.
    /// </summary>
    public struct PowerLevelData : IComponentData
    {
        /// <summary>Current power level.</summary>
        public int Level;

        /// <summary>Maximum power level cap.</summary>
        public int MaxLevel;
    }
}
