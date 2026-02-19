using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Player;

namespace MyGame.Tests
{
    /// <summary>
    /// PlayerMovementSystem 的 EditMode 測試。
    /// 驗證移動邏輯、低速模式切換。
    /// </summary>
    [TestFixture]
    public class PlayerMovementSystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _movementSystemHandle;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            // ISystem 透過 SystemHandle 操作
            _movementSystemHandle = _world.GetOrCreateSystem<PlayerMovementSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        /// <summary>
        /// 建立基本的 Player entity + PlayerInputData singleton。
        /// </summary>
        private (Entity playerEntity, Entity inputEntity) CreatePlayerAndInput(
            float moveSpeed = 10f,
            float focusSpeed = 4f,
            float3? startPos = null)
        {
            var pos = startPos ?? float3.zero;

            // 建立 Player entity
            var player = _em.CreateEntity();
            _em.AddComponentData(player, new PlayerTag());
            _em.AddComponentData(player, new MoveSpeed { Value = moveSpeed });
            _em.AddComponentData(player, new FocusSpeed { Value = focusSpeed });
            _em.AddComponentData(player, LocalTransform.FromPosition(pos));

            // 建立 PlayerInputData singleton
            var input = _em.CreateEntity();
            _em.AddComponentData(input, new PlayerInputData());

            return (player, input);
        }

        private void UpdateMovementSystem()
        {
            _movementSystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void PlayerMoves_WhenMoveInputProvided()
        {
            // Arrange
            var (player, input) = CreatePlayerAndInput(moveSpeed: 10f);

            // 模擬向右輸入
            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(1f, 0f),
                ShootHeld = false,
                FocusHeld = false,
                BombPressed = false
            });

            // Act
            UpdateMovementSystem();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.Greater(pos.x, 0f, "Player should move right when MoveInput.x > 0");
            Assert.AreEqual(0f, pos.y, 0.001f, "Y should remain 0 for horizontal input");
            Assert.AreEqual(0f, pos.z, 0.001f, "Z should always remain 0 (XY plane)");
        }

        [Test]
        public void PlayerDoesNotMove_WhenNoInput()
        {
            // Arrange
            var (player, input) = CreatePlayerAndInput();
            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = float2.zero,
                ShootHeld = false,
                FocusHeld = false,
                BombPressed = false
            });

            // Act
            UpdateMovementSystem();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(0f, pos.x, 0.001f);
            Assert.AreEqual(0f, pos.y, 0.001f);
            Assert.AreEqual(0f, pos.z, 0.001f);
        }

        [Test]
        public void PlayerMovesSlower_WhenFocusHeld()
        {
            // Arrange
            const float normalSpeed = 10f;
            const float focusSpeed = 4f;
            var (player, input) = CreatePlayerAndInput(normalSpeed, focusSpeed);

            // 模擬向上輸入 + Focus
            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(0f, 1f),
                ShootHeld = false,
                FocusHeld = true,
                BombPressed = false
            });

            // Act
            UpdateMovementSystem();

            // 取得 focus 模式下的位移
            var focusPos = _em.GetComponentData<LocalTransform>(player).Position;

            // Reset 位置
            _em.SetComponentData(player, LocalTransform.FromPosition(float3.zero));

            // 模擬向上輸入，不按 Focus
            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(0f, 1f),
                ShootHeld = false,
                FocusHeld = false,
                BombPressed = false
            });

            // Act
            UpdateMovementSystem();
            var normalPos = _em.GetComponentData<LocalTransform>(player).Position;

            // Assert — Focus 模式下移動距離應比正常短
            Assert.Less(focusPos.y, normalPos.y,
                "Focus mode movement should be slower than normal movement");
        }

        [Test]
        public void PlayerMovesOnXYPlane_ZAlwaysZero()
        {
            // Arrange — 從原點開始
            var (player, input) = CreatePlayerAndInput();

            // 模擬斜向輸入
            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(0.7f, 0.7f),
                ShootHeld = false,
                FocusHeld = false,
                BombPressed = false
            });

            // Act
            UpdateMovementSystem();

            // Assert — Z 必須永遠為 0（XY 平面）
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(0f, pos.z, 0.001f, "Z must always be 0 in Touhou-style XY plane");
        }
    }
}
