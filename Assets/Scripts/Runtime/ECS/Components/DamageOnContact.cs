using Unity.Entities;

namespace MyGame.ECS.Collision
{
    /// <summary>
    /// Amount of damage this entity inflicts on contact.
    /// Attached to bullets and enemies that can hurt the player.
    /// </summary>
    public struct DamageOnContact : IComponentData
    {
        /// <summary>Damage dealt to the target on collision.</summary>
        public int Value;
    }
}
