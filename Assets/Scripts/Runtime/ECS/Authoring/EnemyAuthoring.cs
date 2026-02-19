using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using MyGame.ECS.Item;
using MyGame.ECS.Danmaku;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// Authoring component for enemy prefabs.
    /// Baker produces EnemyTag, EnemyVelocity, EnemyShootCooldown,
    /// DanmakuPattern, collision/health, score, and item drop components.
    /// Legacy EnemyBulletPrefabRef/EnemyBulletSpeedData only added when _bulletPrefab is set.
    /// </summary>
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        [Tooltip("Enemy velocity vector (default: downward)")]
        private Vector3 _velocity = new Vector3(0f, -3f, 0f);

        [Header("Shooting")]
        [SerializeField]
        [Tooltip("Legacy bullet prefab (optional, for backward compat)")]
        private GameObject _bulletPrefab;

        [SerializeField]
        [Tooltip("Shoot cooldown in seconds")]
        private float _shootCooldown = 1.0f;

        [SerializeField]
        [Tooltip("Bullet speed")]
        private float _bulletSpeed = 8f;

        [Header("Collision & Health")]
        [SerializeField]
        [Tooltip("Enemy hitbox radius (0.3-0.5 typical)")]
        private float _collisionRadius = 0.4f;

        [SerializeField]
        [Tooltip("Max HP")]
        private int _maxHealth = 3;

        [SerializeField]
        [Tooltip("Contact damage (damage on touching player)")]
        private int _contactDamage = 1;

        [Header("Danmaku Pattern")]
        [SerializeField]
        [Tooltip("Danmaku pattern type")]
        private DanmakuPatternType _danmakuPattern = DanmakuPatternType.Straight;

        [SerializeField]
        [Tooltip("Number of bullets per shot (Fan, Ring, Spread)")]
        private int _bulletCount = 1;

        [SerializeField]
        [Tooltip("Total spread angle in radians (Fan, Spread)")]
        private float _spreadAngle = 1.047f; // ~60 degrees

        [SerializeField]
        [Tooltip("Spiral rotation speed in radians/sec")]
        private float _spiralSpeed = 0.262f; // ~15 degrees

        [SerializeField]
        [Tooltip("Bullet shape")]
        private BulletShape _bulletShape = BulletShape.BallS;

        [SerializeField]
        [Tooltip("Bullet color")]
        private BulletColor _bulletColor = BulletColor.White;

        [SerializeField]
        [Tooltip("Bullet acceleration")]
        private float _bulletAccel = 0f;

        [SerializeField]
        [Tooltip("Bullet max speed (0 = no cap)")]
        private float _bulletMaxSpeed = 0f;

        [SerializeField]
        [Tooltip("Spawn delay in frames before bullet becomes active")]
        private int _spawnDelayFrames = 0;

        [Header("Item Drop")]
        [SerializeField]
        [Tooltip("Item type to drop on death (0=Score, 1=Power, 2=Bomb)")]
        private int _dropType = 0;

        [SerializeField]
        [Tooltip("Drop probability 0.0-1.0")]
        private float _dropChance = 0.3f;

        [Header("Score")]
        [SerializeField]
        [Tooltip("Score awarded on kill")]
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

                // Collision & Health (Phase B)
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

                // Score (Phase C)
                AddComponent(entity, new Score.ScoreOnDeath
                {
                    Value = authoring._scoreValue
                });

                // Shoot cooldown (used by both legacy and danmaku systems)
                AddComponent(entity, new Enemy.EnemyShootCooldown
                {
                    Timer = 0f,
                    Duration = authoring._shootCooldown
                });

                // Legacy prefab-based spawning (backward compat)
                if (authoring._bulletPrefab != null)
                {
                    var bulletPrefabEntity = GetEntity(
                        authoring._bulletPrefab, TransformUsageFlags.Dynamic);

                    AddComponent(entity, new Enemy.EnemyBulletPrefabRef
                    {
                        Value = bulletPrefabEntity
                    });
                    AddComponent(entity, new Enemy.EnemyBulletSpeedData
                    {
                        Value = authoring._bulletSpeed
                    });
                }

                // Item Drop (Phase D)
                AddComponent(entity, new ItemDropData
                {
                    DropType = authoring._dropType,
                    DropChance = authoring._dropChance
                });

                // Danmaku pattern (replaces old BulletPatternData)
                AddComponent(entity, new DanmakuPattern
                {
                    PatternType = authoring._danmakuPattern,
                    Shape = authoring._bulletShape,
                    Color = authoring._bulletColor,
                    Speed = authoring._bulletSpeed,
                    BulletCount = authoring._bulletCount,
                    SpreadAngle = authoring._spreadAngle,
                    SpiralSpeed = authoring._spiralSpeed,
                    Accel = authoring._bulletAccel,
                    MaxSpeed = authoring._bulletMaxSpeed,
                    SpawnDelayFrames = authoring._spawnDelayFrames
                });

                if (authoring._danmakuPattern == DanmakuPatternType.Spiral)
                {
                    AddComponent(entity, new DanmakuSpiralAngle { Value = 0f });
                }
            }
        }
    }
}
