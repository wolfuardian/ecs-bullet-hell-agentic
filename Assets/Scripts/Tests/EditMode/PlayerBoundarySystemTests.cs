using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using MyGame.ECS.Player;
using MyGame.ECS.Boundary;

namespace MyGame.Tests
{
    /// <summary>
    /// PlayerBoundarySystem 的 EditMode 測試。
    /// 驗證玩家位置 clamp、角落處理、Z 軸不變性。
    /// </summary>
    [TestFixture]
    public class PlayerBoundarySystemTests
    {
        private World _world;
        private EntityManager _em;
        private SystemHandle _movementSystemHandle;
        private SystemHandle _boundarySystemHandle;

        /// <summary>測試用固定 DeltaTime（1/60 秒）。</summary>
        private const float TEST_DELTA_TIME = 1f / 60f;

        /// <summary>測試用預設邊界。</summary>
        private static readonly PlayerBoundaryData DEFAULT_BOUNDS = new PlayerBoundaryData
        {
            MinX = -2f,
            MaxX = 2f,
            MinY = -3f,
            MaxY = 3f
        };

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;

            _movementSystemHandle = _world.GetOrCreateSystem<PlayerMovementSystem>();
            _boundarySystemHandle = _world.GetOrCreateSystem<PlayerBoundarySystem>();
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
        /// 建立 Player entity + PlayerInputData singleton + PlayerBoundaryData singleton。
        /// </summary>
        private (Entity playerEntity, Entity inputEntity) CreatePlayerWithBoundary(
            float3? startPos = null,
            PlayerBoundaryData? bounds = null,
            float moveSpeed = 10f,
            float focusSpeed = 4f)
        {
            var pos = startPos ?? float3.zero;

            // Player entity
            var player = _em.CreateEntity();
            _em.AddComponentData(player, new PlayerTag());
            _em.AddComponentData(player, new MoveSpeed { Value = moveSpeed });
            _em.AddComponentData(player, new FocusSpeed { Value = focusSpeed });
            _em.AddComponentData(player, LocalTransform.FromPosition(pos));

            // PlayerInputData singleton
            var input = _em.CreateEntity();
            _em.AddComponentData(input, new PlayerInputData());

            // PlayerBoundaryData singleton
            var boundary = _em.CreateEntity();
            _em.AddComponentData(boundary, bounds ?? DEFAULT_BOUNDS);

            return (player, input);
        }

        /// <summary>
        /// 推進時間並依序更新 Movement → Boundary 系統。
        /// </summary>
        private void UpdateSystems()
        {
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));

            _movementSystemHandle.Update(_world.Unmanaged);
            _boundarySystemHandle.Update(_world.Unmanaged);
        }

        [Test]
        public void PlayerClamped_WhenMovingPastRightBound()
        {
            // Arrange — 靠近右邊界，高速向右移動
            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(1.9f, 0f, 0f));

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(1f, 0f),
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(DEFAULT_BOUNDS.MaxX, pos.x, 0.001f,
                "Player X should be clamped to MaxX");
        }

        [Test]
        public void PlayerClamped_WhenMovingPastLeftBound()
        {
            // Arrange — 靠近左邊界，高速向左移動
            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(-1.9f, 0f, 0f));

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(-1f, 0f),
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(DEFAULT_BOUNDS.MinX, pos.x, 0.001f,
                "Player X should be clamped to MinX");
        }

        [Test]
        public void PlayerClamped_WhenMovingPastTopBound()
        {
            // Arrange — 靠近上邊界，高速向上移動
            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(0f, 2.9f, 0f));

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(0f, 1f),
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(DEFAULT_BOUNDS.MaxY, pos.y, 0.001f,
                "Player Y should be clamped to MaxY");
        }

        [Test]
        public void PlayerClamped_WhenMovingPastBottomBound()
        {
            // Arrange — 靠近下邊界，高速向下移動
            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(0f, -2.9f, 0f));

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(0f, -1f),
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(DEFAULT_BOUNDS.MinY, pos.y, 0.001f,
                "Player Y should be clamped to MinY");
        }

        [Test]
        public void PlayerNotClamped_WhenInsideBounds()
        {
            // Arrange — 在中心，小幅移動不會碰到邊界
            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(0f, 0f, 0f));

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(1f, 0f),
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert — 位置應有變化但不被 clamp
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.Greater(pos.x, 0f, "Player should have moved right");
            Assert.Less(pos.x, DEFAULT_BOUNDS.MaxX,
                "Player should still be within bounds (not clamped)");
        }

        [Test]
        public void PlayerClamped_AtCorner_WhenMovingDiagonallyPastBounds()
        {
            // Arrange — 靠近右上角，斜向推出邊界
            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(1.9f, 2.9f, 0f));

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = new float2(1f, 1f),
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert — 兩軸同時被 clamp
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(DEFAULT_BOUNDS.MaxX, pos.x, 0.001f,
                "Player X should be clamped to MaxX at corner");
            Assert.AreEqual(DEFAULT_BOUNDS.MaxY, pos.y, 0.001f,
                "Player Y should be clamped to MaxY at corner");
        }

        [Test]
        public void PlayerPosition_ZAlwaysZero_AfterBoundaryClamping()
        {
            // Arrange — 手動設定 Z != 0
            var (player, input) = CreatePlayerWithBoundary();
            _em.SetComponentData(player, LocalTransform.FromPosition(new float3(0f, 0f, 5f)));
            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = float2.zero,
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert — Z 必須被強制歸零
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(0f, pos.z, 0.001f,
                "Z should be forced to 0 by boundary system");
        }

        [Test]
        public void PlayerClamped_WhenStartingOutsideBounds()
        {
            // Arrange — 玩家在遠超邊界的位置，無輸入
            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(100f, -50f, 0f));

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = float2.zero,
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert — 位置被 clamp 到邊界內
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(DEFAULT_BOUNDS.MaxX, pos.x, 0.001f,
                "X should be clamped to MaxX");
            Assert.AreEqual(DEFAULT_BOUNDS.MinY, pos.y, 0.001f,
                "Y should be clamped to MinY");
        }

        [Test]
        public void PlayerBoundarySystem_DoesNotRun_WhenNoBoundarySingleton()
        {
            // Arrange — 建立 Player 但不建立 Boundary singleton
            var player = _em.CreateEntity();
            _em.AddComponentData(player, new PlayerTag());
            _em.AddComponentData(player, new MoveSpeed { Value = 10f });
            _em.AddComponentData(player, new FocusSpeed { Value = 4f });
            _em.AddComponentData(player, LocalTransform.FromPosition(new float3(100f, 0f, 0f)));

            var input = _em.CreateEntity();
            _em.AddComponentData(input, new PlayerInputData());

            // Act — 不應 crash（RequireForUpdate 會讓系統 skip）
            var currentTime = _world.Time.ElapsedTime;
            _world.SetTime(new TimeData(
                elapsedTime: currentTime + TEST_DELTA_TIME,
                deltaTime: TEST_DELTA_TIME));
            _boundarySystemHandle.Update(_world.Unmanaged);

            // Assert — 位置不變（系統被跳過）
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(100f, pos.x, 0.001f,
                "Position should be unchanged when no boundary singleton exists");
        }

        [Test]
        public void PlayerClamped_WithCustomBoundaryValues()
        {
            // Arrange — 使用自訂的大範圍邊界
            var customBounds = new PlayerBoundaryData
            {
                MinX = -10f,
                MaxX = 10f,
                MinY = -10f,
                MaxY = 10f
            };

            var (player, input) = CreatePlayerWithBoundary(
                startPos: new float3(15f, 0f, 0f),
                bounds: customBounds);

            _em.SetComponentData(input, new PlayerInputData
            {
                MoveInput = float2.zero,
                FocusHeld = false
            });

            // Act
            UpdateSystems();

            // Assert — 應 clamp 到自訂的 MaxX=10，而非預設的 MaxX=2
            var pos = _em.GetComponentData<LocalTransform>(player).Position;
            Assert.AreEqual(10f, pos.x, 0.001f,
                "Player should be clamped to custom MaxX=10, not default MaxX=2");
        }
    }
}
