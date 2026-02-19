using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using MyGame.ECS.Collision;
using MyGame.ECS.Danmaku;
using MyGame.ECS.Enemy;
using MyGame.ECS.Player;

namespace MyGame.ECS.Bomb
{
    /// <summary>
    /// Handles bomb activation and active bomb timer management.
    /// When player presses bomb and has stock, clears all enemy bullets
    /// (except bomb-immune ones), kills all enemies, and grants invincibility
    /// for the bomb duration.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(InvincibilitySystem))]
    public partial struct BombSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<PlayerInputData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var input = SystemAPI.GetSingleton<PlayerInputData>();

            // Process each player entity with BombData
            foreach (var (bombData, invTimer, entity) in
                SystemAPI.Query<RefRW<BombData>, RefRW<InvincibilityTimer>>()
                    .WithAll<PlayerTag>()
                    .WithEntityAccess())
            {
                // Decrement cooldown timer
                if (bombData.ValueRO.CooldownTimer > 0f)
                {
                    bombData.ValueRW.CooldownTimer -= dt;
                    if (bombData.ValueRW.CooldownTimer < 0f)
                    {
                        bombData.ValueRW.CooldownTimer = 0f;
                    }
                }

                // Part A: Bomb Activation
                if (input.BombPressed
                    && bombData.ValueRO.Stock > 0
                    && bombData.ValueRO.CooldownTimer <= 0f)
                {
                    bombData.ValueRW.Stock--;
                    bombData.ValueRW.CooldownTimer = bombData.ValueRO.CooldownDuration;

                    var bombDuration = bombData.ValueRO.BombDuration;

                    // Add BombActiveData to player
                    ecb.AddComponent(entity, new BombActiveData
                    {
                        Timer = bombDuration
                    });

                    // Grant invincibility
                    invTimer.ValueRW.Value = bombDuration;

                    // Destroy enemy bullets without BulletFlags (always clearable)
                    var normalBulletQuery = SystemAPI.QueryBuilder()
                        .WithAll<EnemyBulletTag>()
                        .WithNone<BulletFlags>()
                        .Build();
                    var normalBulletEntities = normalBulletQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < normalBulletEntities.Length; i++)
                    {
                        ecb.DestroyEntity(normalBulletEntities[i]);
                    }
                    normalBulletEntities.Dispose();

                    // Destroy enemy bullets with BulletFlags, unless bomb-immune
                    var flaggedBulletQuery = SystemAPI.QueryBuilder()
                        .WithAll<EnemyBulletTag, BulletFlags>()
                        .Build();
                    var flaggedBulletEntities = flaggedBulletQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < flaggedBulletEntities.Length; i++)
                    {
                        var flags = state.EntityManager.GetComponentData<BulletFlags>(flaggedBulletEntities[i]);
                        if (!flags.BombImmune)
                        {
                            ecb.DestroyEntity(flaggedBulletEntities[i]);
                        }
                    }
                    flaggedBulletEntities.Dispose();

                    // Add DeadTag to all enemies without DeadTag
                    var enemyQuery = SystemAPI.QueryBuilder()
                        .WithAll<EnemyTag>()
                        .WithNone<DeadTag>()
                        .Build();
                    var enemyEntities = enemyQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < enemyEntities.Length; i++)
                    {
                        ecb.AddComponent<DeadTag>(enemyEntities[i]);
                    }
                    enemyEntities.Dispose();
                }
            }

            // Part B: Bomb Timer Management
            foreach (var (bombActive, entity) in
                SystemAPI.Query<RefRW<BombActiveData>>()
                    .WithAll<PlayerTag>()
                    .WithEntityAccess())
            {
                bombActive.ValueRW.Timer -= dt;
                if (bombActive.ValueRO.Timer <= 0f)
                {
                    ecb.RemoveComponent<BombActiveData>(entity);
                }
            }
        }
    }
}
