using Unity.Entities;

namespace MyGame.ECS.Boundary
{
    /// <summary>
    /// Singleton — defines the rectangular play area that clamps the player position.
    /// MinX/MaxX define horizontal bounds, MinY/MaxY define vertical bounds.
    /// XY plane only (Z is always 0).
    /// </summary>
    public struct PlayerBoundaryData : IComponentData
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
