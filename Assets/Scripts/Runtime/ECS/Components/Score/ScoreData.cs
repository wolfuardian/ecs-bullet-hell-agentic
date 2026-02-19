using Unity.Entities;

namespace MyGame.ECS.Score
{
    /// <summary>
    /// Singleton component holding the current game score.
    /// Exactly one entity with this component should exist in the world.
    /// </summary>
    public struct ScoreData : IComponentData
    {
        /// <summary>Current total score accumulated during this session.</summary>
        public int Value;
    }
}
