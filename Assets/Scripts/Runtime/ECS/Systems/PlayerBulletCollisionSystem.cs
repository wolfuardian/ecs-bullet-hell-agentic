using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Enemy;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// 偵測玩家子彈（PlayerBulletTag）與敵人（EnemyTag）的碰撞。
    /// 命中時：銷毀子彈、扣敵人 HP、HP≤0 時加 DeadTag。
    /// 使用 EndSimulationECB 確保同幀所有碰撞檢查完成後才銷毀。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    [UpdateAfter(typeof(InvincibilitySystem))]
    public partial struct PlayerBulletCollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerBulletTag>();
            state.RequireForUpdate<EnemyTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 收集所有敵人資料到臨時陣列（SystemAPI.Query 不支援巢狀迭代）
            var enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyTag, LocalTransform, CollisionRadius, HealthData>()
                .WithNone<DeadTag>()
                .Build();

            var enemyEntities = enemyQuery.ToEntityArray(Allocator.Temp);
            var enemyTransforms = enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var enemyRadii = enemyQuery.ToComponentDataArray<CollisionRadius>(Allocator.Temp);
            var enemyHealths = enemyQuery.ToComponentDataArray<HealthData>(Allocator.Temp);

            // 追蹤每隻敵人累積的傷害（index-based）
            var enemyDamage = new NativeArray<int>(enemyEntities.Length, Allocator.Temp);
            var destroyedBullets = new NativeHashSet<Entity>(64, Allocator.Temp);

            foreach (var (bulletTransform, bulletRadius, bulletDmg, bulletEntity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>, RefRO<DamageOnContact>>()
                    .WithAll<BulletTag, PlayerBulletTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                if (destroyedBullets.Contains(bulletEntity))
                    continue;

                var bulletPos = bulletTransform.ValueRO.Position;
                var bulletR = bulletRadius.ValueRO.Value;
                var damage = bulletDmg.ValueRO.Value;

                for (int i = 0; i < enemyEntities.Length; i++)
                {
                    var enemyPos = enemyTransforms[i].Position;
                    var enemyR = enemyRadii[i].Value;
                    var radiusSum = bulletR + enemyR;
                    var distSq = math.distancesq(bulletPos, enemyPos);

                    if (distSq <= radiusSum * radiusSum)
                    {
                        // 命中！銷毀子彈
                        ecb.DestroyEntity(bulletEntity);
                        destroyedBullets.Add(bulletEntity);
                        // 累積傷害
                        enemyDamage[i] += damage;
                        break; // 子彈被消耗，不再檢查其他敵人
                    }
                }
            }

            // 套用累積傷害
            for (int i = 0; i < enemyEntities.Length; i++)
            {
                if (enemyDamage[i] > 0)
                {
                    var health = enemyHealths[i];
                    health.Current -= enemyDamage[i];
                    ecb.SetComponent(enemyEntities[i], health);

                    if (health.Current <= 0)
                    {
                        ecb.AddComponent<DeadTag>(enemyEntities[i]);
                    }
                }
            }

            enemyDamage.Dispose();
            destroyedBullets.Dispose();
            enemyEntities.Dispose();
            enemyTransforms.Dispose();
            enemyRadii.Dispose();
            enemyHealths.Dispose();
        }
    }
}
