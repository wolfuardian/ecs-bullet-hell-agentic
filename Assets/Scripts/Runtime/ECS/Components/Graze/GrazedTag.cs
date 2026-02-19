using Unity.Entities;

namespace MyGame.ECS.Graze
{
    /// <summary>
    /// Zero-size tag added to enemy bullets that have already been grazed.
    /// Prevents double-counting the same bullet for graze detection.
    /// </summary>
    public struct GrazedTag : IComponentData
    {
    }
}
