using UnityEngine;
using Unity.Entities;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 掛在場景中玩家 GameObject 上的 Authoring Component。
    /// Baker 會將其轉換為 ECS 的 PlayerTag + MoveSpeed。
    /// </summary>
    public class PlayerAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("玩家移動速度")]
        private float _moveSpeed = 10f;

        public float MoveSpeed => _moveSpeed;

        public class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Player.PlayerTag>(entity);
                AddComponent(entity, new Player.MoveSpeed
                {
                    Value = authoring._moveSpeed
                });
            }
        }
    }
}
