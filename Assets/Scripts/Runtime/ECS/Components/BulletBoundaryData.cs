using Unity.Entities;

namespace MyGame.ECS.Boundary
{
    /// <summary>
    /// Singleton — defines the rectangular area beyond which bullets are destroyed.
    /// Typically larger than PlayerBoundaryData to allow off-screen spawning/travel.
    /// </summary>
    public struct BulletBoundaryData : IComponentData
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
