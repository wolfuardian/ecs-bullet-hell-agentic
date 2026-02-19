using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Beam laser component. Lifecycle: Warning → Grow → Active → Fade.
    /// Collision is a line segment from Origin along Angle * Length.
    /// </summary>
    public struct LaserBeam : IComponentData
    {
        /// <summary>Origin point in world space.</summary>
        public float3 Origin;

        /// <summary>Beam direction angle in radians.</summary>
        public float Angle;

        /// <summary>Current beam length.</summary>
        public float Length;

        /// <summary>Maximum beam length when fully extended.</summary>
        public float MaxLength;

        /// <summary>Beam width for collision and rendering.</summary>
        public float Width;

        /// <summary>How fast the beam extends (units per second).</summary>
        public float GrowSpeed;

        /// <summary>Beam color index.</summary>
        public BulletColor Color;

        /// <summary>True when beam has finished warning phase and is active.</summary>
        public bool Active;

        /// <summary>Warning phase duration remaining in seconds.</summary>
        public float WarningTimer;

        /// <summary>Total active duration in seconds. Destroyed when <= 0.</summary>
        public float Duration;
    }
}
