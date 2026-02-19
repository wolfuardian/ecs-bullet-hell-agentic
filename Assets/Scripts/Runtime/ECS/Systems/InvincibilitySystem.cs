using Unity.Burst;
using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// 每幀遞減 InvincibilityTimer。
    /// 在所有碰撞系統之前執行，確保無敵剛結束的那一幀可立即被擊中。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PlayerBulletCollisionSystem))]
    [UpdateBefore(typeof(EnemyBulletCollisionSystem))]
    [UpdateBefore(typeof(EnemyPlayerCollisionSystem))]
    public partial struct InvincibilitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InvincibilityTimer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var timer in
                SystemAPI.Query<RefRW<InvincibilityTimer>>())
            {
                if (timer.ValueRO.Value > 0f)
                {
                    timer.ValueRW.Value -= dt;
                    if (timer.ValueRW.Value < 0f)
                    {
                        timer.ValueRW.Value = 0f;
                    }
                }
            }
        }
    }
}
