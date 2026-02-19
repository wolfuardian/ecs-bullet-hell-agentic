using UnityEngine;
using Unity.Entities;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 掛在場景中玩家 GameObject 上的 Authoring Component。
    /// Baker 會將其轉換為 ECS 的 PlayerTag + MoveSpeed + FocusSpeed
    /// + BulletPrefabRef + ShootCooldown + BulletSpeedData。
    /// </summary>
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("移動")]
        [SerializeField]
        [Tooltip("正常移動速度")]
        private float _moveSpeed = 10f;

        [SerializeField]
        [Tooltip("低速模式（Focus）移動速度")]
        private float _focusSpeed = 4f;

        [Header("射擊")]
        [SerializeField]
        [Tooltip("子彈 Prefab（需掛有 BulletAuthoring）")]
        private GameObject _bulletPrefab;

        [SerializeField]
        [Tooltip("射擊冷卻時間（秒）")]
        private float _shootCooldown = 0.2f;

        [SerializeField]
        [Tooltip("子彈飛行速度")]
        private float _bulletSpeed = 20f;

        [Header("碰撞與血量")]
        [SerializeField]
        [Tooltip("玩家 hitbox 半徑（東方：極小，0.05-0.1）")]
        private float _collisionRadius = 0.08f;

        [SerializeField]
        [Tooltip("玩家最大 HP（東方：通常為 1，即一擊死亡）")]
        private int _maxHealth = 1;

        [SerializeField]
        [Tooltip("被擊後無敵時間（秒）")]
        private float _invincibilityDuration = 2.0f;

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
                AddComponent(entity, new Player.FocusSpeed
                {
                    Value = authoring._focusSpeed
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
                AddComponent(entity, new Collision.InvincibilityTimer
                {
                    Value = 0f
                });
                AddComponent(entity, new Collision.InvincibilityDuration
                {
                    Value = authoring._invincibilityDuration
                });

                if (authoring._bulletPrefab != null)
                {
                    var bulletPrefabEntity = GetEntity(
                        authoring._bulletPrefab, TransformUsageFlags.Dynamic);

                    AddComponent(entity, new Player.BulletPrefabRef
                    {
                        Value = bulletPrefabEntity
                    });
                    AddComponent(entity, new Player.ShootCooldown
                    {
                        Timer = 0f,
                        Duration = authoring._shootCooldown
                    });
                    AddComponent(entity, new Player.BulletSpeedData
                    {
                        Value = authoring._bulletSpeed
                    });
                }
            }
        }
    }
}
