using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Item;
using MyGame.ECS.Collision;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// Authoring component for item prefab. Bakes into ItemTag, ItemData,
    /// CollisionRadius, ItemLifetime, ItemVelocity, and LocalTransform.
    /// </summary>
    public class ItemAuthoring : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField]
        [Tooltip("Item hitbox radius for player collection")]
        private float _collisionRadius = 0.2f;

        [Header("Lifetime")]
        [SerializeField]
        [Tooltip("Item lifetime in seconds before auto-destruction")]
        private float _lifetime = 10f;

        public class Baker : Baker<ItemAuthoring>
        {
            public override void Bake(ItemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<ItemTag>(entity);
                AddComponent(entity, new ItemData
                {
                    Type = ItemData.SCORE_ITEM,
                    ScoreValue = 100,
                    PowerValue = 1
                });
                AddComponent(entity, new CollisionRadius
                {
                    Value = authoring._collisionRadius
                });
                AddComponent(entity, new ItemLifetime
                {
                    Value = authoring._lifetime
                });
                AddComponent(entity, new ItemVelocity
                {
                    Value = float3.zero
                });
            }
        }
    }
}
