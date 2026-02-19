using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 掛在敵人 Prefab 上的 Authoring Component。
    /// Baker 轉換為 EnemyTag + EnemyVelocity + EnemyShootCooldown
    /// + EnemyBulletPrefabRef + EnemyBulletSpeedData。
    /// </summary>
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("移動")]
        [SerializeField]
        [Tooltip("敵人移動速度向量（預設往下）")]
        private Vector3 _velocity = new Vector3(0f, -3f, 0f);

        [Header("射擊")]
        [SerializeField]
        [Tooltip("敵彈 Prefab（需掛有 BulletAuthoring）")]
        private GameObject _bulletPrefab;

        [SerializeField]
        [Tooltip("射擊冷卻時間（秒）")]
        private float _shootCooldown = 1.0f;

        [SerializeField]
        [Tooltip("敵彈飛行速度")]
        private float _bulletSpeed = 8f;

        public class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<Enemy.EnemyTag>(entity);
                AddComponent(entity, new Enemy.EnemyVelocity
                {
                    Value = new float3(
                        authoring._velocity.x,
                        authoring._velocity.y,
                        authoring._velocity.z)
                });

                if (authoring._bulletPrefab != null)
                {
                    var bulletPrefabEntity = GetEntity(
                        authoring._bulletPrefab, TransformUsageFlags.Dynamic);

                    AddComponent(entity, new Enemy.EnemyBulletPrefabRef
                    {
                        Value = bulletPrefabEntity
                    });
                    AddComponent(entity, new Enemy.EnemyShootCooldown
                    {
                        Timer = 0f,
                        Duration = authoring._shootCooldown
                    });
                    AddComponent(entity, new Enemy.EnemyBulletSpeedData
                    {
                        Value = authoring._bulletSpeed
                    });
                }
            }
        }
    }
}
