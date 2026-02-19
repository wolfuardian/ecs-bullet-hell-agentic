using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Bullet
{
    /// <summary>
    /// 通用速度 Component（世界空間，每秒單位）。
    /// 子彈和未來的敵人彈幕都可使用。
    /// </summary>
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }
}
