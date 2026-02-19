using Unity.Entities;
using Unity.Mathematics;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// Singleton Component，由 PlayerInputSystem（managed）寫入，
    /// 供其他 Burst-compatible System 讀取玩家輸入。
    /// 東方 Project 操作：方向鍵移動、Z 射擊、Shift 低速、X 炸彈。
    /// </summary>
    public struct PlayerInputData : IComponentData
    {
        /// <summary>方向鍵輸入（←↑→↓）</summary>
        public float2 MoveInput;

        /// <summary>射擊鍵是否壓住（Z）</summary>
        public bool ShootHeld;

        /// <summary>低速模式是否壓住（Shift）</summary>
        public bool FocusHeld;

        /// <summary>炸彈鍵是否按下（X）— 單次觸發</summary>
        public bool BombPressed;
    }
}
