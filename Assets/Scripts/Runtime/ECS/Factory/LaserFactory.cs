using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Static factory for creating laser entities via ECB.
    /// </summary>
    public static class LaserFactory
    {
        /// <summary>
        /// Creates a beam laser entity.
        /// Lifecycle: warning (WarningTimer) -> grow (Length increases) -> active -> destroy (Duration).
        /// </summary>
        public static Entity CreateBeam(
            ref EntityCommandBuffer ecb,
            float3 origin, float angle,
            float maxLength, float width,
            float growSpeed, float warningTime,
            float duration, BulletColor color)
        {
            var entity = ecb.CreateEntity();
            ecb.AddComponent<LaserTag>(entity);
            ecb.AddComponent(entity, LocalTransform.FromPosition(origin));
            ecb.AddComponent(entity, new LaserBeam
            {
                Origin = origin,
                Angle = angle,
                Length = 0f,
                MaxLength = maxLength,
                Width = width,
                GrowSpeed = growSpeed,
                Color = color,
                Active = false,
                WarningTimer = warningTime,
                Duration = duration,
            });
            ecb.AddComponent(entity, new DamageOnContact { Value = 1 });
            return entity;
        }

        /// <summary>
        /// Creates a curve laser entity with initial head point.
        /// Head moves using BulletMotion; new points are prepended each frame.
        /// </summary>
        public static Entity CreateCurveLaser(
            ref EntityCommandBuffer ecb,
            float3 origin, float speed, float angle,
            float width, int segmentCount,
            float duration, BulletColor color)
        {
            var entity = ecb.CreateEntity();
            ecb.AddComponent<LaserTag>(entity);
            ecb.AddComponent(entity, LocalTransform.FromPosition(origin));
            ecb.AddComponent(entity, new CurveLaser
            {
                Width = width,
                Color = color,
                SegmentCount = segmentCount,
                Duration = duration,
            });
            ecb.AddComponent(entity, new BulletMotion
            {
                Speed = speed,
                Angle = angle,
                Accel = 0f,
                MaxSpeed = 0f,
                AngularVel = 0f,
            });
            ecb.AddComponent(entity, new DamageOnContact { Value = 1 });

            // Add buffer with initial point
            var buffer = ecb.AddBuffer<CurveLaserPoint>(entity);
            buffer.Add(new CurveLaserPoint
            {
                Position = origin,
                Velocity = new float3(math.cos(angle), math.sin(angle), 0f) * speed,
            });

            return entity;
        }
    }
}
