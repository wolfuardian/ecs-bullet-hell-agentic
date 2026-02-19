using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Bit flags for special bullet properties.
    /// </summary>
    public struct BulletFlags : IComponentData
    {
        /// <summary>Raw flag bits.</summary>
        public byte Value;

        /// <summary>Bullet cannot be cleared by bombs.</summary>
        public const byte BOMB_IMMUNE  = 1 << 0;

        /// <summary>Bullet persists across scenes / special transitions.</summary>
        public const byte PERSISTENT   = 1 << 1;

        public bool BombImmune
        {
            get => (Value & BOMB_IMMUNE) != 0;
            set => Value = value ? (byte)(Value | BOMB_IMMUNE) : (byte)(Value & ~BOMB_IMMUNE);
        }

        public bool Persistent
        {
            get => (Value & PERSISTENT) != 0;
            set => Value = value ? (byte)(Value | PERSISTENT) : (byte)(Value & ~PERSISTENT);
        }
    }
}
