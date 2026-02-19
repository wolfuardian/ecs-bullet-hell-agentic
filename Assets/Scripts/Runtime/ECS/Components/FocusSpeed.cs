using Unity.Entities;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// 低速（Focus）模式下的移動速度。
    /// 壓住 Shift 時使用此值，放開則使用 MoveSpeed。
    /// </summary>
    public struct FocusSpeed : IComponentData
    {
        public float Value;
    }
}
