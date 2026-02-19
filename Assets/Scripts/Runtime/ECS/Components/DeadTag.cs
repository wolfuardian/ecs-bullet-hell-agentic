using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Zero-size tag marking an entity for destruction by DeathSystem.
    /// Applied when HealthData.Current reaches 0 or below.
    /// Two-phase death: collision system sets HP to 0 and adds DeadTag,
    /// DeathSystem destroys entities with DeadTag at end of frame.
    /// </summary>
    public struct DeadTag : IComponentData
    {
    }
}
