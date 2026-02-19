using Unity.Entities;

namespace MyGame.ECS.Danmaku
{
    /// <summary>
    /// Polar-based motion for danmaku bullets.
    /// Replaces cartesian Velocity for enemy bullets with richer control.
    /// Movement formula: pos += (cos(Angle), sin(Angle), 0) * Speed * dt.
    /// </summary>
    public struct BulletMotion : IComponentData
    {
        /// <summary>Current speed in world units per second.</summary>
        public float Speed;

        /// <summary>Current movement angle in radians.</summary>
        public float Angle;

        /// <summary>Speed change per second. Positive = accelerate.</summary>
        public float Accel;

        /// <summary>Speed cap when accelerating. 0 = no cap.</summary>
        public float MaxSpeed;

        /// <summary>Angular velocity in radians per second. Used for curving/homing.</summary>
        public float AngularVel;
    }
}
