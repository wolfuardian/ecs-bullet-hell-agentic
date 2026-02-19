using Unity.Entities;

namespace MyGame.ECS.Item
{
    /// <summary>
    /// Singleton holding item prefab entity reference.
    /// </summary>
    public struct ItemPrefabRef : IComponentData
    {
        /// <summary>Item prefab entity for instantiation.</summary>
        public Entity Prefab;
    }
}
