using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using MyGame.ECS.Boundary;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 銷毀超出 BulletBoundaryData 矩形範圍的敵人 Entity。
    /// 敵人從畫面上方進入、往下移動，超出底部時銷毀。
    /// 四面都檢查以防敵人橫向飄出。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial struct EnemyBoundarySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
            state.RequireForUpdate<BulletBoundaryData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bounds = SystemAPI.GetSingleton<BulletBoundaryData>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (transform, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<EnemyTag>()
                    .WithEntityAccess())
            {
                var pos = transform.ValueRO.Position;

                if (pos.x < bounds.MinX || pos.x > bounds.MaxX ||
                    pos.y < bounds.MinY || pos.y > bounds.MaxY)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
