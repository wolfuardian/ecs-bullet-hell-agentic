using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using MyGame.ECS.Boundary;

namespace MyGame.ECS.Bullet
{
    /// <summary>
    /// 銷毀超出 BulletBoundaryData 矩形範圍的子彈 Entity。
    /// 比等待 Lifetime 歸零更有效率 — 飛出畫面的子彈立即回收。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    [UpdateBefore(typeof(BulletLifetimeSystem))]
    public partial struct BulletBoundarySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BulletTag>();
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
                    .WithAll<BulletTag>()
                    .WithEntityAccess())
            {
                var pos = transform.ValueRO.Position;

                // AABB 範圍外 → 排程銷毀
                if (pos.x < bounds.MinX || pos.x > bounds.MaxX ||
                    pos.y < bounds.MinY || pos.y > bounds.MaxY)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
