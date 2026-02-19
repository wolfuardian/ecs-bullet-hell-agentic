using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Visual appearance of a danmaku bullet: shape + color.
    /// Used by rendering systems to select the correct sprite from the sheet.
    /// </summary>
    public struct BulletVisual : IComponentData
    {
        /// <summary>Shape index (row in sprite sheet).</summary>
        public BulletShape Shape;

        /// <summary>Color index (column in sprite sheet).</summary>
        public BulletColor Color;
    }
}
