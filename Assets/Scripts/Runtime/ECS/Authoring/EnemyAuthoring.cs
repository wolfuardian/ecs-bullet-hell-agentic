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

        [Header("碰撞與血量")]
        [SerializeField]
        [Tooltip("敵人 hitbox 半徑（0.3-0.5 典型值）")]
        private float _collisionRadius = 0.4f;

        [SerializeField]
        [Tooltip("敵人最大 HP")]
        private int _maxHealth = 3;

        [SerializeField]
        [Tooltip("體碰傷害（碰到玩家時造成的傷害）")]
        private int _contactDamage = 1;

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

                // 碰撞與血量（Phase B）
                AddComponent(entity, new Collision.CollisionRadius
                {
                    Value = authoring._collisionRadius
                });
                AddComponent(entity, new Collision.HealthData
                {
                    Current = authoring._maxHealth,
                    Max = authoring._maxHealth
                });
                AddComponent(entity, new Collision.DamageOnContact
                {
                    Value = authoring._contactDamage
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
