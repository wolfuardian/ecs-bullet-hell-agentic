namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Bullet visual shape index. Maps to rows in Shot_01/02 sprite sheets.
    /// 23 shapes: 19 from design doc + BigBall, YinYangS/M/L from sprites.
    /// </summary>
    public enum BulletShape : byte
    {
        Pellet      = 0,
        BallS       = 1,
        RingBall    = 2,
        BallM       = 3,
        RiceS       = 4,
        Kunai       = 5,
        Scale       = 6,
        Ofuda       = 7,
        StarS       = 8,
        StarM       = 9,
        Cross       = 10,
        Oval        = 11,
        Arrow       = 12,
        Knife       = 13,
        Butterfly   = 14,
        Needle      = 15,
        Bullet      = 16,
        Glow        = 17,
        LaserSeg    = 18,
        BigBall     = 19,
        YinYangS    = 20,
        YinYangM    = 21,
        YinYangL    = 22,
    }

    /// <summary>
    /// Bullet color index (0..15). Each shape row has 16 color columns.
    /// </summary>
    public enum BulletColor : byte
    {
        White       = 0,
        Red         = 1,
        Orange      = 2,
        Yellow      = 3,
        LightGreen  = 4,
        Green       = 5,
        Cyan        = 6,
        LightBlue   = 7,
        Blue        = 8,
        DarkBlue    = 9,
        Purple      = 10,
        Magenta     = 11,
        Pink        = 12,
        DarkRed     = 13,
        DarkGreen   = 14,
        DarkPurple  = 15,
    }

    /// <summary>
    /// Hitbox geometry type for collision detection.
    /// </summary>
    public enum HitboxType : byte
    {
        Circle = 0,
        Oval   = 1,
        Rect   = 2,
        Line   = 3,
        None   = 4,
    }

    /// <summary>
    /// How the bullet sprite rotates.
    /// </summary>
    public enum RotateMode : byte
    {
        /// <summary>No rotation (e.g., symmetric shapes like balls).</summary>
        Fixed    = 0,
        /// <summary>Sprite faces movement direction.</summary>
        Velocity = 1,
        /// <summary>Sprite spins at a constant rate.</summary>
        Spin     = 2,
    }
}
