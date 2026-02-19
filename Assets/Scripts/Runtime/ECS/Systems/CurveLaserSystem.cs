using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Updates curve laser control points each frame.
    /// Moves the head entity using BulletMotion, inserts new points,
    /// drifts existing points, trims buffer to SegmentCount, and handles duration.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CurveLaserSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CurveLaser>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (curve, motion, transform, entity) in
                SystemAPI.Query<RefRW<CurveLaser>, RefRW<BulletMotion>,
                    RefRW<LocalTransform>>()
                    .WithAll<LaserTag, CurveLaserPoint>()
                    .WithEntityAccess())
            {
                ref var m = ref motion.ValueRW;
                ref var c = ref curve.ValueRW;

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

                // Move head entity
                var dir = new float3(math.cos(m.Angle), math.sin(m.Angle), 0f);
                transform.ValueRW.Position += dir * m.Speed * dt;

                // Get mutable buffer via EntityManager
                var buffer = state.EntityManager.GetBuffer<CurveLaserPoint>(entity);

                // Drift existing points by their individual velocities
                for (int i = 0; i < buffer.Length; i++)
                {
                    var pt = buffer[i];
                    pt.Position += pt.Velocity * dt;
                    buffer[i] = pt;
                }

                // Insert new head point at index 0
                var headVelocity = dir * m.Speed;
                var newPoint = new CurveLaserPoint
                {
                    Position = transform.ValueRO.Position,
                    Velocity = headVelocity,
                };

                // Shift existing elements and insert at front
                buffer.Add(default); // grow by one
                for (int i = buffer.Length - 1; i > 0; i--)
                {
                    buffer[i] = buffer[i - 1];
                }
                buffer[0] = newPoint;

                // Trim buffer to SegmentCount
                while (buffer.Length > c.SegmentCount)
                {
                    buffer.RemoveAt(buffer.Length - 1);
                }

                // Duration countdown
                c.Duration -= dt;
                if (c.Duration <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
