using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 掛在子彈 Prefab 上的 Authoring Component。
    /// Baker 會轉換為 BulletTag + BulletLifetime + Velocity。
    /// </summary>
    public class BulletAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("子彈飛行速度")]
        private float _speed = 20f;

        [SerializeField]
        [Tooltip("子彈存活時間（秒）")]
        private float _lifetime = 3f;

        public float Speed => _speed;
        public float Lifetime => _lifetime;

        public class Baker : Baker<BulletAuthoring>
        {
            public override void Bake(BulletAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Bullet.BulletTag>(entity);
                AddComponent(entity, new Bullet.BulletLifetime
                {
                    Value = authoring._lifetime
                });
                // Velocity 初始為零，由 BulletSpawnSystem 在 Instantiate 後設定實際方向
                AddComponent(entity, new Bullet.Velocity
                {
                    Value = float3.zero
                });
            }
        }
    }
}
