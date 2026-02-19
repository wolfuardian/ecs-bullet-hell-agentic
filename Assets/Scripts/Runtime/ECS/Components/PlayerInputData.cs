using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// Singleton Component，由 PlayerInputSystem（managed）寫入，
    /// 供其他 Burst-compatible System 讀取玩家輸入。
    /// </summary>
    public struct PlayerInputData : IComponentData
    {
        public float2 MoveInput;
        public bool AttackPressed;
    }
}
