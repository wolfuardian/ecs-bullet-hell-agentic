using NUnit.Framework;
using Unity.Entities;
using MyGame.ECS.Collision;

namespace MyGame.Tests
{
    /// <summary>
    /// Collision 相關 Component 的結構驗證測試。
    /// 確認所有新 Component 可正確建立、設值、讀值。
    /// </summary>
    [TestFixture]
    public class CollisionComponentTests
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
        public void CollisionRadius_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new CollisionRadius { Value = 0.4f });

            var data = _em.GetComponentData<CollisionRadius>(entity);
            Assert.AreEqual(0.4f, data.Value, 0.001f,
                "CollisionRadius should store the assigned value");
        }

        [Test]
        public void HealthData_StoresCurrentAndMax()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new HealthData { Current = 3, Max = 5 });

            var data = _em.GetComponentData<HealthData>(entity);
            Assert.AreEqual(3, data.Current, "HealthData.Current should be 3");
            Assert.AreEqual(5, data.Max, "HealthData.Max should be 5");
        }

        [Test]
        public void DamageOnContact_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new DamageOnContact { Value = 2 });

            var data = _em.GetComponentData<DamageOnContact>(entity);
            Assert.AreEqual(2, data.Value,
                "DamageOnContact should store the assigned value");
        }

        [Test]
        public void InvincibilityTimer_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new InvincibilityTimer { Value = 1.5f });

            var data = _em.GetComponentData<InvincibilityTimer>(entity);
            Assert.AreEqual(1.5f, data.Value, 0.001f,
                "InvincibilityTimer should store the assigned value");
        }

        [Test]
        public void InvincibilityDuration_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new InvincibilityDuration { Value = 2.0f });

            var data = _em.GetComponentData<InvincibilityDuration>(entity);
            Assert.AreEqual(2.0f, data.Value, 0.001f,
                "InvincibilityDuration should store the assigned value");
        }

        [Test]
        public void PlayerBulletTag_IsZeroSizeComponent()
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlayerBulletTag>(entity);

            Assert.IsTrue(_em.HasComponent<PlayerBulletTag>(entity),
                "Entity should have PlayerBulletTag after adding it");
        }

        [Test]
        public void EnemyBulletTag_IsZeroSizeComponent()
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<EnemyBulletTag>(entity);

            Assert.IsTrue(_em.HasComponent<EnemyBulletTag>(entity),
                "Entity should have EnemyBulletTag after adding it");
        }

        [Test]
        public void DeadTag_IsZeroSizeComponent()
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<DeadTag>(entity);

            Assert.IsTrue(_em.HasComponent<DeadTag>(entity),
                "Entity should have DeadTag after adding it");
        }
    }
}
