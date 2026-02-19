using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;
using MyGame.ECS.Danmaku;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// Legacy prefab-based enemy bullet spawner.
    /// Only processes enemies with EnemyBulletPrefabRef that do NOT have DanmakuPattern.
    /// Enemies with DanmakuPattern are handled by DanmakuPatternSystem instead.
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
                    .WithAll<EnemyTag>()
                    .WithNone<DanmakuPattern>())
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
                // Phase B: 標記為敵人子彈，用於碰撞系統區分
                ecb.AddComponent<EnemyBulletTag>(bulletEntity);
            }
        }
    }
}
