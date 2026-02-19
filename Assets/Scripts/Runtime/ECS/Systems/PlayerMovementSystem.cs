using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// 讀取 PlayerInputData singleton，移動帶有 PlayerTag 的 Entity。
    /// XY 平面移動（東方 Project 風格縱向 STG）。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<PlayerInputData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var input = SystemAPI.GetSingleton<PlayerInputData>();
            var dt = SystemAPI.Time.DeltaTime;

            // XY 平面：input.x → world.x, input.y → world.y
            var moveDir = new float3(input.MoveInput.x, input.MoveInput.y, 0f);

            foreach (var (transform, speed) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>()
                    .WithAll<PlayerTag>())
            {
                transform.ValueRW.Position += moveDir * speed.ValueRO.Value * dt;
            }
        }
    }
}
