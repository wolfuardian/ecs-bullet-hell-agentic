using Unity.Entities;

namespace MyGame.ECS.Wave
{
    /// <summary>
    /// [DEPRECATED] Use MyGame.ECS.Danmaku.DanmakuPattern instead.
    /// Kept for backward compatibility with legacy BulletPatternSystem.
    /// </summary>
    public struct BulletPatternData : IComponentData
    {
        /// <summary>Fire a single bullet straight down.</summary>
        public const int STRAIGHT = 0;

        /// <summary>Fire multiple bullets in a fan spread.</summary>
        public const int FAN = 1;

        /// <summary>Fire bullets in a rotating spiral pattern.</summary>
        public const int SPIRAL = 2;

        /// <summary>Fire a bullet aimed at the player position.</summary>
        public const int AIMED = 3;

        /// <summary>Which pattern to use (see constants).</summary>
        public int PatternType;

        /// <summary>Number of bullets per shot (used by FAN pattern).</summary>
        public int BulletCount;

        /// <summary>Total spread angle in degrees (used by FAN pattern).</summary>
        public float SpreadAngle;

        /// <summary>Rotation speed in degrees per shot (used by SPIRAL pattern).</summary>
        public float SpiralSpeed;
    }
}
