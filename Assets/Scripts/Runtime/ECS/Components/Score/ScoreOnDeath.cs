using Unity.Entities;

namespace MyGame.ECS.Score
{
    /// <summary>
    /// Attached to enemies. Points awarded when this entity is destroyed.
    /// ScoreSystem reads this value when the entity also has DeadTag.
    /// </summary>
    public struct ScoreOnDeath : IComponentData
    {
        /// <summary>Points to award when this entity dies.</summary>
        public int Value;
    }
}
