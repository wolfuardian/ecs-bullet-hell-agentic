using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Enemy;
using MyGame.ECS.Player;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// 偵測敵人（EnemyTag）體碰玩家（PlayerTag）的碰撞。
    /// 命中時：扣玩家 HP、啟動無敵時間、HP≤0 時加 DeadTag。
    /// 敵人不會因體碰而被銷毀（東方慣例）。
    /// 無敵中跳過所有檢查。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    [UpdateAfter(typeof(InvincibilitySystem))]
    [UpdateAfter(typeof(EnemyBulletCollisionSystem))]
    public partial struct EnemyPlayerCollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<EnemyTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 取得玩家資料
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
                break;
            }

            if (!playerFound || invTimer > 0f)
                return;

            foreach (var (enemyTransform, enemyRadius, enemyDmg) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>, RefRO<DamageOnContact>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                var enemyPos = enemyTransform.ValueRO.Position;
                var enemyR = enemyRadius.ValueRO.Value;
                var radiusSum = playerR + enemyR;
                var distSq = math.distancesq(playerPos, enemyPos);

                if (distSq <= radiusSum * radiusSum)
                {
                    var damage = enemyDmg.ValueRO.Value;
                    var newHp = playerHp - damage;
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
                        ecb.SetComponent(playerEntity, new InvincibilityTimer
                        {
                            Value = invDuration
                        });
                    }

                    break; // 一幀只算一次命中（東方慣例）
                }
            }
        }
    }
}
