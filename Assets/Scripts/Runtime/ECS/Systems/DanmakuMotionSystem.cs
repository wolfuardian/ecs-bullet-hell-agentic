using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Moves bullets using polar-based BulletMotion data.
    /// Applies acceleration, angular velocity, and translates position each frame.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct DanmakuMotionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BulletMotion>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, motion) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<BulletMotion>>()
                    .WithAll<BulletTag>())
            {
                ref var m = ref motion.ValueRW;

                // Apply acceleration
                if (m.Accel != 0f)
                {
                    m.Speed += m.Accel * dt;
                    if (m.MaxSpeed > 0f)
                        m.Speed = math.min(m.Speed, m.MaxSpeed);
                }

                // Apply angular velocity
                if (m.AngularVel != 0f)
                    m.Angle += m.AngularVel * dt;

                // Polar to cartesian movement
                var dir = new float3(math.cos(m.Angle), math.sin(m.Angle), 0f);
                transform.ValueRW.Position += dir * m.Speed * dt;
            }
        }
    }
}
