using Unity.Entities;

namespace MyGame.ECS.Bomb
{
    /// <summary>
    /// Added to player when bomb is active, removed when duration expires.
    /// While present, the bomb effect is visually and mechanically active.
    /// </summary>
    public struct BombActiveData : IComponentData
    {
        /// <summary>Remaining active time in seconds.</summary>
        public float Timer;
    }
}
