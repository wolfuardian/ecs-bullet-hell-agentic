using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Boundary;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// 將玩家位置 clamp 在 PlayerBoundaryData 定義的矩形範圍內。
    /// 在 PlayerMovementSystem 之後執行，確保玩家永遠不會渲染在邊界外。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial struct PlayerBoundarySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<PlayerBoundaryData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bounds = SystemAPI.GetSingleton<PlayerBoundaryData>();

            foreach (var transform in
                SystemAPI.Query<RefRW<LocalTransform>>()
                    .WithAll<PlayerTag>())
            {
                var pos = transform.ValueRO.Position;
                pos.x = math.clamp(pos.x, bounds.MinX, bounds.MaxX);
                pos.y = math.clamp(pos.y, bounds.MinY, bounds.MaxY);
                // XY 平面遊戲 — 強制 Z = 0
                pos.z = 0f;
                transform.ValueRW.Position = pos;
            }
        }
    }
}
