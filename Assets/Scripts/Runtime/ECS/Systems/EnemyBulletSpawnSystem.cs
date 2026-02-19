using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 遞減敵人射擊冷卻，歸零時以 ECB 生成敵彈 Entity。
    /// 敵彈往 -Y 方向飛（東方 Project 風格：敵人從上往下射）。
    /// 敵彈掛有 BulletTag + Velocity + BulletLifetime，自動被既有子彈系統處理。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyBulletSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, prefabRef, cooldown, bulletSpeed) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyBulletPrefabRef>,
                    RefRW<EnemyShootCooldown>, RefRO<EnemyBulletSpeedData>>()
                    .WithAll<EnemyTag>())
            {
                // 遞減冷卻計時器
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRO.Timer > 0f)
                    continue;

                // 重置冷卻
                cooldown.ValueRW.Timer = cooldown.ValueRO.Duration;

                // 在敵人位置下方一點生成子彈
                var spawnPos = transform.ValueRO.Position + new float3(0f, -0.5f, 0f);
                var speed = bulletSpeed.ValueRO.Value;

                var bulletEntity = ecb.Instantiate(prefabRef.ValueRO.Value);
                ecb.SetComponent(bulletEntity, LocalTransform.FromPosition(spawnPos));
                ecb.SetComponent(bulletEntity, new Velocity
                {
                    Value = new float3(0f, -speed, 0f)
                });
            }
        }
    }
}
