using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 週期性生成敵人 Entity。讀取 EnemySpawnerData singleton，
    /// Timer 歸零時在隨機 X 位置、固定 Y 位置生成一隻敵人。
    /// 使用 Unity.Mathematics.Random 產生偽隨機 X，Burst 相容。
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemySpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemySpawnerData>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var spawner in
                SystemAPI.Query<RefRW<EnemySpawnerData>>())
            {
                spawner.ValueRW.Timer -= dt;
                if (spawner.ValueRO.Timer > 0f)
                    continue;

                // 重置計時器
                spawner.ValueRW.Timer = spawner.ValueRO.Interval;

                var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

                // 偽隨機 X 位置（基於當前時間的 hash）
                var seed = (uint)((SystemAPI.Time.ElapsedTime + 1.0) * 10000.0) | 1u;
                var rng = Unity.Mathematics.Random.CreateFromIndex(seed);
                var spawnX = rng.NextFloat(spawner.ValueRO.SpawnMinX, spawner.ValueRO.SpawnMaxX);
                var spawnPos = new float3(spawnX, spawner.ValueRO.SpawnY, 0f);

                var enemy = ecb.Instantiate(spawner.ValueRO.Prefab);
                ecb.SetComponent(enemy, LocalTransform.FromPosition(spawnPos));
            }
        }
    }
}
