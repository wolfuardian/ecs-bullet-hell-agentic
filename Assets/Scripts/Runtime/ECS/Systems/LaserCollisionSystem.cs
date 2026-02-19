using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Collision;
using MyGame.ECS.Player;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Detects laser vs player collision for both beam and curve lasers.
    /// Follows Touhou convention: only one hit per frame.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LaserBeamSystem))]
    [UpdateAfter(typeof(CurveLaserSystem))]
    public partial struct LaserCollisionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<LaserTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Get player data (single player assumption)
            float3 playerPos = default;
            float playerR = 0f;
            int playerHp = 0;
            int playerMaxHp = 0;
            float invTimer = 0f;
            float invDuration = 0f;
            Entity playerEntity = Entity.Null;
            bool playerFound = false;

            foreach (var (transform, radius, health, invincibility, invDur, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<CollisionRadius>,
                    RefRO<HealthData>, RefRO<InvincibilityTimer>, RefRO<InvincibilityDuration>>()
                    .WithAll<PlayerTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                playerPos = transform.ValueRO.Position;
                playerR = radius.ValueRO.Value;
                playerHp = health.ValueRO.Current;
                playerMaxHp = health.ValueRO.Max;
                invTimer = invincibility.ValueRO.Value;
                invDuration = invDur.ValueRO.Value;
                playerEntity = entity;
                playerFound = true;
                break; // single player
            }

            if (!playerFound || invTimer > 0f)
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            bool playerHit = false;
            int totalDamage = 0;

            // Beam laser collision: point-to-line-segment distance
            foreach (var (beam, dmg, entity) in
                SystemAPI.Query<RefRO<LaserBeam>, RefRO<DamageOnContact>>()
                    .WithAll<LaserTag>()
                    .WithEntityAccess())
            {
                if (playerHit) break;

                ref readonly var b = ref beam.ValueRO;

                // Skip inactive (warning phase) beams
                if (!b.Active)
                    continue;

                // Skip zero-length beams
                if (b.Length <= 0f)
                    continue;

                // Line segment: Origin to Origin + dir * Length
                var dir = new float3(math.cos(b.Angle), math.sin(b.Angle), 0f);
                var segStart = b.Origin;
                var segEnd = b.Origin + dir * b.Length;

                float dist = PointToSegmentDistance(playerPos, segStart, segEnd);
                float hitThreshold = b.Width * 0.5f + playerR;

                if (dist <= hitThreshold)
                {
                    totalDamage += dmg.ValueRO.Value;
                    playerHit = true;
                }
            }

            // Curve laser collision: check each segment between adjacent points
            if (!playerHit)
            {
                foreach (var (curve, dmg, buffer, entity) in
                    SystemAPI.Query<RefRO<CurveLaser>, RefRO<DamageOnContact>,
                        DynamicBuffer<CurveLaserPoint>>()
                        .WithAll<LaserTag>()
                        .WithEntityAccess())
                {
                    if (playerHit) break;

                    ref readonly var c = ref curve.ValueRO;
                    float hitThreshold = c.Width * 0.5f + playerR;

                    for (int i = 0; i < buffer.Length - 1; i++)
                    {
                        var segStart = buffer[i].Position;
                        var segEnd = buffer[i + 1].Position;

                        float dist = PointToSegmentDistance(playerPos, segStart, segEnd);
                        if (dist <= hitThreshold)
                        {
                            totalDamage += dmg.ValueRO.Value;
                            playerHit = true;
                            break;
                        }
                    }
                }
            }

            // Apply damage to player
            if (playerHit)
            {
                var newHp = playerHp - totalDamage;
                ecb.SetComponent(playerEntity, new HealthData
                {
                    Current = newHp,
                    Max = playerMaxHp
                });

                if (newHp <= 0)
                {
                    ecb.AddComponent<DeadTag>(playerEntity);
                }
                else
                {
                    ecb.SetComponent(playerEntity, new InvincibilityTimer
                    {
                        Value = invDuration
                    });
                }
            }
        }

        /// <summary>
        /// Computes the shortest distance from point P to line segment AB.
        /// </summary>
        private static float PointToSegmentDistance(float3 p, float3 a, float3 b)
        {
            var ab = b - a;
            var ap = p - a;
            float abLenSq = math.lengthsq(ab);

            // Degenerate segment (zero length)
            if (abLenSq < 1e-8f)
                return math.length(ap);

            // Project P onto line AB, clamped to [0,1]
            float t = math.saturate(math.dot(ap, ab) / abLenSq);
            var closest = a + ab * t;
            return math.length(p - closest);
        }
    }
}
