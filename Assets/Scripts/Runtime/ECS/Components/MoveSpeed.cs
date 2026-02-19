using Unity.Entities;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// 通用移動速度 Component，玩家和敵人都可使用。
    /// </summary>
    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }
}
