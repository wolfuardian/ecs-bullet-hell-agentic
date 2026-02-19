using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Player;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// 偵測敵人子彈（EnemyBulletTag）與玩家（PlayerTag）的碰撞。
    /// 命中時：銷毀子彈、扣玩家 HP、啟動無敵時間、HP≤0 時加 DeadTag。
    /// 無敵中（InvincibilityTimer > 0）跳過所有檢查。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    [UpdateAfter(typeof(InvincibilitySystem))]
    public partial struct EnemyBulletCollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<EnemyBulletTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 取得玩家資料（假設單一玩家）
            float3 playerPos = default;
            float playerR = 0f;
            int playerHp = 0;
            int playerMaxHp = 0;
            float invTimer = 0f;
            float invDuration = 0f;
            Entity playerEntity = Entity.Null;
            bool playerFound = false;

            foreach (var (transform, radius, health, invincibility, invDur, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>,
                    RefRO<HealthData>, RefRO<InvincibilityTimer>, RefRO<InvincibilityDuration>>()
                    .WithAll<PlayerTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                playerPos = transform.ValueRO.Position;
                playerR = radius.ValueRO.Value;
                playerHp = health.ValueRO.Current;
                playerMaxHp = health.ValueRO.Max;
                invTimer = invincibility.ValueRO.Value;
                invDuration = invDur.ValueRO.Value;
                playerEntity = entity;
                playerFound = true;
                break; // 單一玩家
            }

            if (!playerFound || invTimer > 0f)
                return; // 玩家無敵或已死——跳過

            bool playerHit = false;
            int totalDamage = 0;

            foreach (var (bulletTransform, bulletRadius, bulletDmg, bulletEntity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>, RefRO<DamageOnContact>>()
                    .WithAll<BulletTag, EnemyBulletTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                var bulletPos = bulletTransform.ValueRO.Position;
                var bulletR = bulletRadius.ValueRO.Value;
                var radiusSum = playerR + bulletR;
                var distSq = math.distancesq(playerPos, bulletPos);

                if (distSq <= radiusSum * radiusSum)
                {
                    ecb.DestroyEntity(bulletEntity);
                    totalDamage += bulletDmg.ValueRO.Value;
                    playerHit = true;
                    // 東方慣例：一幀只算一次命中（無敵會在下一幀生效）
                    break;
                }
            }

            if (playerHit)
            {
                var newHp = playerHp - totalDamage;
                ecb.SetComponent(playerEntity, new HealthData
                {
                    Current = newHp,
                    Max = playerMaxHp
                });

                if (newHp <= 0)
                {
                    ecb.AddComponent<DeadTag>(playerEntity);
                }
                else
                {
                    // 授予無敵時間
                    ecb.SetComponent(playerEntity, new InvincibilityTimer
                    {
                        Value = invDuration
                    });
                }
            }
        }
    }
}
