using Unity.Mathematics;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Hitbox definition for a bullet shape: geometry type + dimensions.
    /// </summary>
    public struct HitboxDef
    {
        public HitboxType Type;
        public float2 Size;
        public float2 Offset;
    }

    /// <summary>
    /// Static definition for one bullet shape.
    /// Contains sprite sheet info, rendering, and hitbox data.
    /// </summary>
    public struct BulletShapeDef
    {
        /// <summary>Row index in the sprite sheet.</summary>
        public byte Row;

        /// <summary>Number of animation frames (1 = static).</summary>
        public byte FrameCount;

        /// <summary>Render sorting order (higher = on top).</summary>
        public byte RenderOrder;

        /// <summary>How the sprite rotates.</summary>
        public RotateMode Rotate;

        /// <summary>Cell size in pixels (width, height).</summary>
        public int2 CellSize;

        /// <summary>Hitbox definition for this shape.</summary>
        public HitboxDef Hitbox;

        /// <summary>Spin speed in radians/sec (only if Rotate == Spin).</summary>
        public float SpinSpeed;
    }
}
