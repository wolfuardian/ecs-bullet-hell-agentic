using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Curve laser head entity. Control points are stored in a
    /// DynamicBuffer&lt;CurveLaserPoint&gt; on the same entity.
    /// </summary>
    public struct CurveLaser : IComponentData
    {
        /// <summary>Width of each laser segment for collision and rendering.</summary>
        public float Width;

        /// <summary>Color index for the laser segments.</summary>
        public BulletColor Color;

        /// <summary>Maximum number of segments (buffer capacity hint).</summary>
        public int SegmentCount;

        /// <summary>Remaining lifetime in seconds. Destroyed when <= 0.</summary>
        public float Duration;
    }
}
