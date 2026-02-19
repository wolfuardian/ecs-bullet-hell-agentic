using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Buffer element for curve laser control points.
    /// Each point has its own position and velocity, creating a snake-like curve.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct CurveLaserPoint : IBufferElementData
    {
        /// <summary>World position of this control point.</summary>
        public float3 Position;

        /// <summary>Velocity of this control point (updated each frame).</summary>
        public float3 Velocity;
    }
}
