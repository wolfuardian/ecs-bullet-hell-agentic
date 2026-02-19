using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// Managed SystemBase，橋接 New Input System → ECS。
    /// 使用 InputSystem_Actions（由 .inputactions Generate C# Class 產生）
    /// 將玩家輸入寫入 PlayerInputData singleton，
    /// 供 Burst-compatible System 讀取。
    ///
    /// 東方 Project 操作：
    /// - 方向鍵 ←↑→↓：移動自機
    /// - Z 壓住：連續射擊
    /// - Shift 壓住：低速模式
    /// - X 按下：使用炸彈
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        private @InputSystem_Actions _inputActions;
        private Entity _inputEntity;

        protected override void OnCreate()
        {
            // 使用 .inputactions 產生的 C# wrapper class
            _inputActions = new @InputSystem_Actions();
            _inputActions.Player.Enable();

            // 建立 singleton entity 持有 PlayerInputData
            _inputEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(_inputEntity, new PlayerInputData());

#if UNITY_EDITOR
            EntityManager.SetName(_inputEntity, "PlayerInput");
#endif
        }

        protected override void OnUpdate()
        {
            var moveValue = _inputActions.Player.Move.ReadValue<Vector2>();
            var shootHeld = _inputActions.Player.Shoot.IsPressed();
            var focusHeld = _inputActions.Player.Focus.IsPressed();
            var bombPressed = _inputActions.Player.Bomb.WasPressedThisFrame();

            EntityManager.SetComponentData(_inputEntity, new PlayerInputData
            {
                MoveInput = new float2(moveValue.x, moveValue.y),
                ShootHeld = shootHeld,
                FocusHeld = focusHeld,
                BombPressed = bombPressed
            });
        }

        protected override void OnDestroy()
        {
            if (_inputActions != null)
            {
                _inputActions.Player.Disable();
                _inputActions.Dispose();
                _inputActions = null;
            }
        }
    }
}
