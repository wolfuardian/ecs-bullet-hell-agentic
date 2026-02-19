using UnityEngine;
using Unity.Entities;
using MyGame.ECS.Item;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// Singleton authoring that holds the item prefab reference.
    /// Place on a single GameObject in the scene.
    /// </summary>
    public class ItemSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Item Prefab (must have ItemAuthoring)")]
        private GameObject _itemPrefab;

        public class Baker : Baker<ItemSpawnerAuthoring>
        {
            public override void Bake(ItemSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                if (authoring._itemPrefab != null)
                {
                    var prefabEntity = GetEntity(
                        authoring._itemPrefab, TransformUsageFlags.Dynamic);

                    AddComponent(entity, new ItemPrefabRef
                    {
                        Prefab = prefabEntity
                    });
                }
            }
        }
    }
}
