using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// Managed SystemBase，橋接 New Input System → ECS。
    /// 將玩家輸入寫入 PlayerInputData singleton，
    /// 供 Burst-compatible System 讀取。
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        private InputAction _moveAction;
        private InputAction _attackAction;
        private Entity _inputEntity;

        protected override void OnCreate()
        {
            // 以程式碼定義 Input Action，避免依賴 Resources.Load
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Gamepad>/leftStick/up")
                .With("Down", "<Gamepad>/leftStick/down")
                .With("Left", "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right");

            _attackAction = new InputAction("Attack", InputActionType.Button);
            _attackAction.AddBinding("<Mouse>/leftButton");
            _attackAction.AddBinding("<Gamepad>/rightTrigger");

            _moveAction.Enable();
            _attackAction.Enable();

            // 建立 singleton entity 持有 PlayerInputData
            _inputEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(_inputEntity, new PlayerInputData());

#if UNITY_EDITOR
            EntityManager.SetName(_inputEntity, "PlayerInput");
#endif
        }

        protected override void OnUpdate()
        {
            var moveValue = _moveAction.ReadValue<Vector2>();
            var attackPressed = _attackAction.IsPressed();

            EntityManager.SetComponentData(_inputEntity, new PlayerInputData
            {
                MoveInput = new float2(moveValue.x, moveValue.y),
                AttackPressed = attackPressed
            });
        }

        protected override void OnDestroy()
        {
            _moveAction?.Disable();
            _moveAction?.Dispose();
            _attackAction?.Disable();
            _attackAction?.Dispose();
        }
    }
}
