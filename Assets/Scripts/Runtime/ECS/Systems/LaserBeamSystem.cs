using Unity.Burst;
using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Manages beam laser lifecycle: warning phase -> growth phase -> active duration -> destroy.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct LaserBeamSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LaserBeam>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (beam, entity) in
                SystemAPI.Query<RefRW<LaserBeam>>()
                    .WithAll<LaserTag>()
                    .WithEntityAccess())
            {
                ref var b = ref beam.ValueRW;

                // Phase 1: Warning — beam is not active yet
                if (!b.Active)
                {
                    b.WarningTimer -= dt;
                    if (b.WarningTimer <= 0f)
                    {
                        b.Active = true;
                    }
                    continue; // no growth during warning
                }

                // Phase 2: Growth — extend beam toward max length
                if (b.Length < b.MaxLength)
                {
                    b.Length += b.GrowSpeed * dt;
                    if (b.Length > b.MaxLength)
                        b.Length = b.MaxLength;
                }

                // Phase 3: Duration countdown — destroy when expired
                b.Duration -= dt;
                if (b.Duration <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
