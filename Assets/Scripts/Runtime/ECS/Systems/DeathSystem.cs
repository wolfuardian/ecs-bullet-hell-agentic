using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// 銷毀所有標記 DeadTag 的 Entity。
    /// 在所有碰撞/傷害系統之後執行，確保狀態一致。
    /// 使用 EndSimulationECB 延遲銷毀。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerBulletCollisionSystem))]
    [UpdateAfter(typeof(EnemyBulletCollisionSystem))]
    [UpdateAfter(typeof(EnemyPlayerCollisionSystem))]
    public partial struct DeathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DeadTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var query = SystemAPI.QueryBuilder().WithAll<DeadTag>().Build();
            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                ecb.DestroyEntity(entities[i]);
            }

            entities.Dispose();
        }
    }
}
