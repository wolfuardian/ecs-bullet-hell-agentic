using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Collision;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Static factory for creating danmaku bullet entities via ECB.
    /// All bullets get: LocalTransform, BulletTag, EnemyBulletTag, BulletVisual,
    /// BulletMotion, BulletHitbox, BulletLifetime, DamageOnContact, BulletFlags.
    /// SpawnDelay added only if delay > 0.
    /// </summary>
    public static class BulletFactory
    {
        private const float DEFAULT_LIFETIME = 10f;
        private const int DEFAULT_DAMAGE = 1;

        private static Entity CreateBase(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float angle,
            BulletShape shape, BulletColor color,
            int delay, float accel = 0f, float maxSpeed = 0f, float angularVel = 0f)
        {
            var entity = ecb.CreateEntity();
            ecb.AddComponent(entity, LocalTransform.FromPosition(pos));
            ecb.AddComponent<BulletTag>(entity);
            ecb.AddComponent<EnemyBulletTag>(entity);
            ecb.AddComponent(entity, new BulletVisual { Shape = shape, Color = color });
            ecb.AddComponent(entity, new BulletMotion
            {
                Speed = speed,
                Angle = angle,
                Accel = accel,
                MaxSpeed = maxSpeed,
                AngularVel = angularVel
            });

            var shapeDef = BulletShapeTable.Get(shape);
            ecb.AddComponent(entity, new BulletHitbox
            {
                Type = shapeDef.Hitbox.Type,
                Size = shapeDef.Hitbox.Size,
                Offset = shapeDef.Hitbox.Offset
            });

            ecb.AddComponent(entity, new BulletLifetime { Value = DEFAULT_LIFETIME });
            ecb.AddComponent(entity, new DamageOnContact { Value = DEFAULT_DAMAGE });
            ecb.AddComponent(entity, new BulletFlags { Value = 0 });

            if (delay > 0)
                ecb.AddComponent(entity, new SpawnDelay { FramesRemaining = delay });

            return entity;
        }

        /// <summary>
        /// Single bullet at given angle.
        /// </summary>
        public static Entity Shot(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float angle,
            BulletShape shape, BulletColor color,
            int delay = 0)
        {
            return CreateBase(ref ecb, pos, speed, angle, shape, color, delay);
        }

        /// <summary>
        /// Accelerating bullet with speed cap.
        /// </summary>
        public static Entity ShotAccel(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float angle,
            float accel, float maxSpeed,
            BulletShape shape, BulletColor color,
            int delay = 0)
        {
            return CreateBase(ref ecb, pos, speed, angle, shape, color, delay,
                accel: accel, maxSpeed: maxSpeed);
        }

        /// <summary>
        /// Homing bullet that curves toward target via AngularVel.
        /// </summary>
        public static Entity ShotHoming(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float angle,
            float turnRate,
            BulletShape shape, BulletColor color,
            int delay = 0)
        {
            var entity = CreateBase(ref ecb, pos, speed, angle, shape, color, delay,
                angularVel: turnRate);
            ecb.AddComponent<HomingTag>(entity);
            return entity;
        }

        /// <summary>
        /// Fan of bullets spread evenly around a center angle.
        /// </summary>
        public static void ShotFan(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float centerAngle, float spreadAngle,
            int count,
            BulletShape shape, BulletColor color,
            int delay = 0)
        {
            if (count <= 0) return;

            if (count == 1)
            {
                CreateBase(ref ecb, pos, speed, centerAngle, shape, color, delay);
                return;
            }

            float step = spreadAngle / (count - 1);
            float startAngle = centerAngle - spreadAngle * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * step;
                CreateBase(ref ecb, pos, speed, angle, shape, color, delay);
            }
        }

        /// <summary>
        /// Full 360-degree ring of bullets.
        /// </summary>
        public static void ShotRing(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float startAngle,
            int count,
            BulletShape shape, BulletColor color,
            int delay = 0)
        {
            if (count <= 0) return;

            float step = math.PI * 2f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * step;
                CreateBase(ref ecb, pos, speed, angle, shape, color, delay);
            }
        }

        /// <summary>
        /// Random spread of bullets around a base angle with speed variation.
        /// </summary>
        public static void ShotSpread(
            ref EntityCommandBuffer ecb,
            float3 pos, float baseAngle, float randomRange,
            float2 speedRange, int count,
            BulletShape shape, BulletColor color,
            int delay = 0, uint seed = 1u)
        {
            if (count <= 0) return;

            var rng = Random.CreateFromIndex(seed);
            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle + rng.NextFloat(-randomRange, randomRange);
                float speed = rng.NextFloat(speedRange.x, speedRange.y);
                CreateBase(ref ecb, pos, speed, angle, shape, color, delay);
            }
        }

        /// <summary>
        /// Single bullet aimed at target position.
        /// </summary>
        public static Entity ShotAim(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float3 targetPos,
            BulletShape shape, BulletColor color,
            int delay = 0)
        {
            float angle = math.atan2(targetPos.y - pos.y, targetPos.x - pos.x);
            return CreateBase(ref ecb, pos, speed, angle, shape, color, delay);
        }

        /// <summary>
        /// Fan of bullets aimed at target position.
        /// </summary>
        public static void ShotAimFan(
            ref EntityCommandBuffer ecb,
            float3 pos, float speed, float3 targetPos,
            float spreadAngle, int count,
            BulletShape shape, BulletColor color,
            int delay = 0)
        {
            float centerAngle = math.atan2(targetPos.y - pos.y, targetPos.x - pos.x);
            ShotFan(ref ecb, pos, speed, centerAngle, spreadAngle, count, shape, color, delay);
        }
    }
}
