using Unity.Entities;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Item remaining lifetime in seconds. Destroyed at 0.
    /// </summary>
    public struct ItemLifetime : IComponentData
    {
        /// <summary>Remaining lifetime in seconds.</summary>
        public float Value;
    }
}
