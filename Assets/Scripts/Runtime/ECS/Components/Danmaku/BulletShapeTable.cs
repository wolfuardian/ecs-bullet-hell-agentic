using Unity.Mathematics;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Static lookup table for all 23 bullet shapes.
    /// O(1) access via BulletShape enum index.
    /// Hitbox sizes are in world units (approximate Touhou scale).
    /// </summary>
    public static class BulletShapeTable
    {
        public const int SHAPE_COUNT = 23;

        private static readonly BulletShapeDef[] _table = new BulletShapeDef[SHAPE_COUNT]
        {
            // 0: Pellet — tiny round dot
            new BulletShapeDef
            {
                Row = 0, FrameCount = 1, RenderOrder = 10,
                Rotate = RotateMode.Fixed,
                CellSize = new int2(8, 8),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.04f, 0f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 1: BallS — small ball
            new BulletShapeDef
            {
                Row = 1, FrameCount = 1, RenderOrder = 20,
                Rotate = RotateMode.Fixed,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.06f, 0f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 2: RingBall — hollow ring ball
            new BulletShapeDef
            {
                Row = 2, FrameCount = 1, RenderOrder = 20,
                Rotate = RotateMode.Fixed,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.06f, 0f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 3: BallM — medium ball
            new BulletShapeDef
            {
                Row = 3, FrameCount = 1, RenderOrder = 25,
                Rotate = RotateMode.Fixed,
                CellSize = new int2(32, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.10f, 0f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 4: RiceS — small rice grain (elongated)
            new BulletShapeDef
            {
                Row = 4, FrameCount = 1, RenderOrder = 15,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 8),
                Hitbox = new HitboxDef { Type = HitboxType.Oval, Size = new float2(0.06f, 0.03f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 5: Kunai — throwing knife shape
            new BulletShapeDef
            {
                Row = 5, FrameCount = 1, RenderOrder = 15,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Oval, Size = new float2(0.07f, 0.03f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 6: Scale — fish scale / leaf shape
            new BulletShapeDef
            {
                Row = 6, FrameCount = 1, RenderOrder = 15,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Oval, Size = new float2(0.06f, 0.04f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 7: Ofuda — talisman / card
            new BulletShapeDef
            {
                Row = 7, FrameCount = 1, RenderOrder = 15,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Rect, Size = new float2(0.04f, 0.10f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 8: StarS — small star
            new BulletShapeDef
            {
                Row = 8, FrameCount = 1, RenderOrder = 20,
                Rotate = RotateMode.Spin,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.05f, 0f), Offset = float2.zero },
                SpinSpeed = 6.28f, // ~1 rotation per second
            },
            // 9: StarM — medium star
            new BulletShapeDef
            {
                Row = 9, FrameCount = 1, RenderOrder = 25,
                Rotate = RotateMode.Spin,
                CellSize = new int2(32, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.08f, 0f), Offset = float2.zero },
                SpinSpeed = 4.71f, // ~0.75 rotations per second
            },
            // 10: Cross — cross / plus shape
            new BulletShapeDef
            {
                Row = 10, FrameCount = 1, RenderOrder = 20,
                Rotate = RotateMode.Spin,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.06f, 0f), Offset = float2.zero },
                SpinSpeed = 6.28f,
            },
            // 11: Oval — oval / ellipse bullet
            new BulletShapeDef
            {
                Row = 11, FrameCount = 1, RenderOrder = 15,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 24),
                Hitbox = new HitboxDef { Type = HitboxType.Oval, Size = new float2(0.05f, 0.08f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 12: Arrow — arrow bullet
            new BulletShapeDef
            {
                Row = 12, FrameCount = 1, RenderOrder = 15,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Line, Size = new float2(0.10f, 0.02f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 13: Knife — large knife / dagger
            new BulletShapeDef
            {
                Row = 13, FrameCount = 1, RenderOrder = 15,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Rect, Size = new float2(0.04f, 0.12f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 14: Butterfly — butterfly-shaped bullet
            new BulletShapeDef
            {
                Row = 14, FrameCount = 4, RenderOrder = 25,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(32, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.08f, 0f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 15: Needle — very thin long needle
            new BulletShapeDef
            {
                Row = 15, FrameCount = 1, RenderOrder = 10,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(8, 48),
                Hitbox = new HitboxDef { Type = HitboxType.Line, Size = new float2(0.15f, 0.01f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 16: Bullet — generic bullet shape
            new BulletShapeDef
            {
                Row = 16, FrameCount = 1, RenderOrder = 20,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Oval, Size = new float2(0.06f, 0.04f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 17: Glow — glowing orb (additive blend)
            new BulletShapeDef
            {
                Row = 17, FrameCount = 1, RenderOrder = 30,
                Rotate = RotateMode.Fixed,
                CellSize = new int2(32, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.08f, 0f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 18: LaserSeg — single laser segment (for curve lasers)
            new BulletShapeDef
            {
                Row = 18, FrameCount = 1, RenderOrder = 5,
                Rotate = RotateMode.Velocity,
                CellSize = new int2(16, 16),
                Hitbox = new HitboxDef { Type = HitboxType.Line, Size = new float2(0.08f, 0.03f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 19: BigBall — large ball (大玉)
            new BulletShapeDef
            {
                Row = 19, FrameCount = 1, RenderOrder = 35,
                Rotate = RotateMode.Fixed,
                CellSize = new int2(64, 64),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.20f, 0f), Offset = float2.zero },
                SpinSpeed = 0f,
            },
            // 20: YinYangS — small yin-yang (陰陽玉S)
            new BulletShapeDef
            {
                Row = 20, FrameCount = 1, RenderOrder = 30,
                Rotate = RotateMode.Spin,
                CellSize = new int2(32, 32),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.10f, 0f), Offset = float2.zero },
                SpinSpeed = 3.14f,
            },
            // 21: YinYangM — medium yin-yang (陰陽玉M)
            new BulletShapeDef
            {
                Row = 21, FrameCount = 1, RenderOrder = 33,
                Rotate = RotateMode.Spin,
                CellSize = new int2(48, 48),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.15f, 0f), Offset = float2.zero },
                SpinSpeed = 2.36f,
            },
            // 22: YinYangL — large yin-yang (陰陽玉L)
            new BulletShapeDef
            {
                Row = 22, FrameCount = 1, RenderOrder = 35,
                Rotate = RotateMode.Spin,
                CellSize = new int2(64, 64),
                Hitbox = new HitboxDef { Type = HitboxType.Circle, Size = new float2(0.20f, 0f), Offset = float2.zero },
                SpinSpeed = 1.57f,
            },
        };

        /// <summary>
        /// O(1) lookup of shape definition by enum value.
        /// </summary>
        public static ref readonly BulletShapeDef Get(BulletShape shape)
        {
            return ref _table[(int)shape];
        }
    }
}
