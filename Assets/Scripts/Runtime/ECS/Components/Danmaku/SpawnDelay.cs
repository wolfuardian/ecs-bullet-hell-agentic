using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// While FramesRemaining > 0 the bullet is invisible and has no collision.
    /// Decremented each frame by SpawnDelaySystem.
    /// </summary>
    public struct SpawnDelay : IComponentData
    {
        /// <summary>Frames remaining before the bullet becomes active.</summary>
        public int FramesRemaining;
    }
}
