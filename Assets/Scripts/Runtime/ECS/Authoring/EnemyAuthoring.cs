using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using MyGame.ECS.Item;
using MyGame.ECS.Wave;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 掛在敵人 Prefab 上的 Authoring Component。
    /// Baker 轉換為 EnemyTag + EnemyVelocity + EnemyShootCooldown
    /// + EnemyBulletPrefabRef + EnemyBulletSpeedData + ScoreOnDeath。
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

        [Header("Bullet Pattern")]
        [SerializeField]
        [Tooltip("Bullet pattern type: 0=STRAIGHT, 1=FAN, 2=SPIRAL, 3=AIMED")]
        private int _patternType = 0;

        [SerializeField]
        [Tooltip("Number of bullets per shot (used by FAN pattern)")]
        private int _bulletCount = 1;

        [SerializeField]
        [Tooltip("Total spread angle in degrees (used by FAN pattern)")]
        private float _spreadAngle = 60f;

        [SerializeField]
        [Tooltip("Rotation speed in degrees per shot (used by SPIRAL pattern)")]
        private float _spiralSpeed = 15f;

        [Header("Item Drop")]
        [SerializeField]
        [Tooltip("Item type to drop on death (0=Score, 1=Power, 2=Bomb)")]
        private int _dropType = 0;

        [SerializeField]
        [Tooltip("Drop probability 0.0-1.0")]
        private float _dropChance = 0.3f;

        [Header("Score")]
        [SerializeField]
        [Tooltip("擊殺此敵人獲得的分數")]
        private int _scoreValue = 100;

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

                // Score（Phase C）
                AddComponent(entity, new Score.ScoreOnDeath
                {
                    Value = authoring._scoreValue
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

                // Item Drop（Phase D）
                AddComponent(entity, new ItemDropData
                {
                    DropType = authoring._dropType,
                    DropChance = authoring._dropChance
                });

                // Bullet pattern (Wave system)
                AddComponent(entity, new BulletPatternData
                {
                    PatternType = authoring._patternType,
                    BulletCount = authoring._bulletCount,
                    SpreadAngle = authoring._spreadAngle,
                    SpiralSpeed = authoring._spiralSpeed
                });

                if (authoring._patternType == BulletPatternData.SPIRAL)
                {
                    AddComponent(entity, new SpiralAngle { Value = 0f });
                }
            }
        }
    }
}
