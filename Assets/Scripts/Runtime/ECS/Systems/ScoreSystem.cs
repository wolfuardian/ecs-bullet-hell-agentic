using Unity.Burst;
using Unity.Entities;
using MyGame.ECS.Collision;

namespace MyGame.ECS.Score
{
    /// <summary>
    /// Sums ScoreOnDeath values from all entities marked with DeadTag
    /// and adds the total to the ScoreData singleton.
    /// Runs after collision systems (which add DeadTag) and before
    /// DeathSystem (which destroys the entities).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyPlayerCollisionSystem))]
    [UpdateBefore(typeof(DeathSystem))]
    public partial struct ScoreSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ScoreData>();
            state.RequireForUpdate<DeadTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int totalPoints = 0;

            foreach (var scoreOnDeath in
                SystemAPI.Query<RefRO<ScoreOnDeath>>().WithAll<DeadTag>())
            {
                totalPoints += scoreOnDeath.ValueRO.Value;
            }

            if (totalPoints > 0)
            {
                var scoreData = SystemAPI.GetSingletonRW<ScoreData>();
                scoreData.ValueRW.Value += totalPoints;
            }
        }
    }
}
