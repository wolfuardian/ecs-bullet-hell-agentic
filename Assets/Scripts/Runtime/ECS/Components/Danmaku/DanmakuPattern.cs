using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Danmaku pattern type constants. Replaces old BulletPatternData.
    /// </summary>
    public enum DanmakuPatternType : byte
    {
        Straight = 0,
        Fan      = 1,
        Spiral   = 2,
        Aimed    = 3,
        Ring     = 4,
        Spread   = 5,
    }

    /// <summary>
    /// Replaces old BulletPatternData with richer danmaku configuration.
    /// Attached to enemy entities to define how DanmakuPatternSystem spawns bullets.
    /// </summary>
    public struct DanmakuPattern : IComponentData
    {
        /// <summary>Which pattern to use.</summary>
        public DanmakuPatternType PatternType;

        /// <summary>Bullet visual shape.</summary>
        public BulletShape Shape;

        /// <summary>Bullet visual color.</summary>
        public BulletColor Color;

        /// <summary>Bullet speed in world units per second.</summary>
        public float Speed;

        /// <summary>Number of bullets per shot (Fan, Ring, Spread).</summary>
        public int BulletCount;

        /// <summary>Total spread angle in radians (Fan, Spread).</summary>
        public float SpreadAngle;

        /// <summary>Angular velocity for spiral pattern (radians/sec).</summary>
        public float SpiralSpeed;

        /// <summary>Acceleration applied to bullets (for accel patterns).</summary>
        public float Accel;

        /// <summary>Max speed cap for accelerating bullets. 0 = no cap.</summary>
        public float MaxSpeed;

        /// <summary>Spawn delay in frames before bullet becomes active.</summary>
        public int SpawnDelayFrames;
    }
}
