using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Zero-size tag marking a bullet as homing.
    /// HomingSystem will rotate the bullet's BulletMotion.Angle toward the player.
    /// </summary>
    public struct HomingTag : IComponentData
    {
    }
}
