using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Bullet;
using MyGame.ECS.Player;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Rotates homing bullets toward the player.
    /// Only affects bullets with both HomingTag and BulletMotion.
    /// Uses shortest rotation path and respects AngularVel magnitude.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DanmakuMotionSystem))]
    public partial struct HomingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HomingTag>();
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Find the player position
            float3 playerPos = float3.zero;
            foreach (var transform in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<PlayerTag>())
            {
                playerPos = transform.ValueRO.Position;
                break;
            }

            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, motion) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<BulletMotion>>()
                    .WithAll<BulletTag, HomingTag>())
            {
                ref var m = ref motion.ValueRW;
                var bulletPos = transform.ValueRO.Position;

                // Calculate target angle from bullet to player
                var delta = playerPos - bulletPos;
                var targetAngle = math.atan2(delta.y, delta.x);

                // Calculate shortest rotation direction
                var angleDiff = targetAngle - m.Angle;

                // Normalize to [-PI, PI]
                angleDiff = angleDiff - 2f * math.PI * math.round(angleDiff / (2f * math.PI));

                // Apply angular velocity toward target, clamped by AngularVel magnitude
                var maxTurn = math.abs(m.AngularVel) * dt;
                var turn = math.clamp(angleDiff, -maxTurn, maxTurn);
                m.Angle += turn;
            }
        }
    }
}
