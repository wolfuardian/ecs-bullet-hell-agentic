using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using MyGame.ECS.Player;
using MyGame.ECS.Bullet;

namespace MyGame.Tests
{
    /// <summary>
    /// Component 資料結構的基本驗證測試。
    /// 確保 Component 可正確建立、設值、讀取。
    /// </summary>
    [TestFixture]
    public class ComponentDataTests
    {
        private World _world;
        private EntityManager _em;

        [SetUp]
        public void SetUp()
        {
            _world = new World("TestWorld");
            _em = _world.EntityManager;
        }

        [TearDown]
        public void TearDown()
        {
            if (_world != null && _world.IsCreated)
            {
                _world.Dispose();
            }
        }

        [Test]
        public void PlayerInputData_DefaultValues_AreZero()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new PlayerInputData());

            var data = _em.GetComponentData<PlayerInputData>(entity);
            Assert.AreEqual(float2.zero, data.MoveInput);
            Assert.IsFalse(data.ShootHeld);
            Assert.IsFalse(data.FocusHeld);
            Assert.IsFalse(data.BombPressed);
        }

        [Test]
        public void PlayerInputData_CanSetAndReadAllFields()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new PlayerInputData
            {
                MoveInput = new float2(0.5f, -0.3f),
                ShootHeld = true,
                FocusHeld = true,
                BombPressed = true
            });

            var data = _em.GetComponentData<PlayerInputData>(entity);
            Assert.AreEqual(0.5f, data.MoveInput.x, 0.001f);
            Assert.AreEqual(-0.3f, data.MoveInput.y, 0.001f);
            Assert.IsTrue(data.ShootHeld);
            Assert.IsTrue(data.FocusHeld);
            Assert.IsTrue(data.BombPressed);
        }

        [Test]
        public void MoveSpeed_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new MoveSpeed { Value = 10f });

            var speed = _em.GetComponentData<MoveSpeed>(entity);
            Assert.AreEqual(10f, speed.Value, 0.001f);
        }

        [Test]
        public void FocusSpeed_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new FocusSpeed { Value = 4f });

            var speed = _em.GetComponentData<FocusSpeed>(entity);
            Assert.AreEqual(4f, speed.Value, 0.001f);
        }

        [Test]
        public void ShootCooldown_StoresTimerAndDuration()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new ShootCooldown
            {
                Timer = 0.1f,
                Duration = 0.2f
            });

            var cd = _em.GetComponentData<ShootCooldown>(entity);
            Assert.AreEqual(0.1f, cd.Timer, 0.001f);
            Assert.AreEqual(0.2f, cd.Duration, 0.001f);
        }

        [Test]
        public void BulletLifetime_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new BulletLifetime { Value = 3f });

            var lifetime = _em.GetComponentData<BulletLifetime>(entity);
            Assert.AreEqual(3f, lifetime.Value, 0.001f);
        }

        [Test]
        public void Velocity_StoresFloat3Value()
        {
            var entity = _em.CreateEntity();
            var expected = new float3(1f, 20f, 0f);
            _em.AddComponentData(entity, new Velocity { Value = expected });

            var vel = _em.GetComponentData<Velocity>(entity);
            Assert.AreEqual(expected.x, vel.Value.x, 0.001f);
            Assert.AreEqual(expected.y, vel.Value.y, 0.001f);
            Assert.AreEqual(expected.z, vel.Value.z, 0.001f);
        }

        [Test]
        public void BulletPrefabRef_StoresEntityReference()
        {
            var prefabEntity = _em.CreateEntity();
            var playerEntity = _em.CreateEntity();
            _em.AddComponentData(playerEntity, new BulletPrefabRef { Value = prefabEntity });

            var prefabRef = _em.GetComponentData<BulletPrefabRef>(playerEntity);
            Assert.AreEqual(prefabEntity, prefabRef.Value);
        }

        [Test]
        public void PlayerTag_IsZeroSizeComponent()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new PlayerTag());

            Assert.IsTrue(_em.HasComponent<PlayerTag>(entity));
        }

        [Test]
        public void BulletTag_IsZeroSizeComponent()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new BulletTag());

            Assert.IsTrue(_em.HasComponent<BulletTag>(entity));
        }
    }
}
