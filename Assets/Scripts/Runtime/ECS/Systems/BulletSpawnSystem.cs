using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace MyGame.ECS.Bullet
{
    /// <summary>
    /// 讀取玩家攻擊輸入，以 ECB 生成子彈 Entity。
    /// 子彈往 +Y 方向飛（東方 Project 風格縱向 STG）。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct BulletSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player.PlayerTag>();
            state.RequireForUpdate<Player.PlayerInputData>();
            state.RequireForUpdate<Player.BulletPrefabRef>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<Player.PlayerInputData>();
            if (!input.AttackPressed)
                return;

            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, prefabRef, cooldown, bulletSpeed) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<Player.BulletPrefabRef>,
                    RefRW<Player.ShootCooldown>, RefRO<Player.BulletSpeedData>>()
                    .WithAll<Player.PlayerTag>())
            {
                // 遞減冷卻計時器
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRO.Timer > 0f)
                    continue;

                // 重置冷卻
                cooldown.ValueRW.Timer = cooldown.ValueRO.Duration;

                // 在玩家位置上方一點生成子彈
                var spawnPos = transform.ValueRO.Position + new float3(0f, 0.5f, 0f);
                var speed = bulletSpeed.ValueRO.Value;

                var bulletEntity = ecb.Instantiate(prefabRef.ValueRO.Value);
                ecb.SetComponent(bulletEntity, LocalTransform.FromPosition(spawnPos));
                ecb.SetComponent(bulletEntity, new Velocity
                {
                    Value = new float3(0f, speed, 0f)
                });
            }
        }
    }
}
