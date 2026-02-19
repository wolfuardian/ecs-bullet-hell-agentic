using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using MyGame.ECS.Enemy;

namespace MyGame.Tests
{
    /// <summary>
    /// Enemy Component 資料結構的基本驗證測試。
    /// 確保所有 Enemy Component 可正確建立、設值、讀取。
    /// </summary>
    [TestFixture]
    public class EnemyComponentTests
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
        public void EnemyTag_IsZeroSizeComponent()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new EnemyTag());

            Assert.IsTrue(_em.HasComponent<EnemyTag>(entity));
        }

        [Test]
        public void EnemyVelocity_StoresFloat3Value()
        {
            var entity = _em.CreateEntity();
            var expected = new float3(1f, -3f, 0f);
            _em.AddComponentData(entity, new EnemyVelocity { Value = expected });

            var vel = _em.GetComponentData<EnemyVelocity>(entity);
            Assert.AreEqual(expected.x, vel.Value.x, 0.001f);
            Assert.AreEqual(expected.y, vel.Value.y, 0.001f);
            Assert.AreEqual(expected.z, vel.Value.z, 0.001f);
        }

        [Test]
        public void EnemyShootCooldown_StoresTimerAndDuration()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new EnemyShootCooldown
            {
                Timer = 0.5f,
                Duration = 1.0f
            });

            var cd = _em.GetComponentData<EnemyShootCooldown>(entity);
            Assert.AreEqual(0.5f, cd.Timer, 0.001f);
            Assert.AreEqual(1.0f, cd.Duration, 0.001f);
        }

        [Test]
        public void EnemyBulletPrefabRef_StoresEntityReference()
        {
            var prefabEntity = _em.CreateEntity();
            var enemyEntity = _em.CreateEntity();
            _em.AddComponentData(enemyEntity, new EnemyBulletPrefabRef { Value = prefabEntity });

            var prefabRef = _em.GetComponentData<EnemyBulletPrefabRef>(enemyEntity);
            Assert.AreEqual(prefabEntity, prefabRef.Value);
        }

        [Test]
        public void EnemyBulletSpeedData_StoresValue()
        {
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new EnemyBulletSpeedData { Value = 8f });

            var data = _em.GetComponentData<EnemyBulletSpeedData>(entity);
            Assert.AreEqual(8f, data.Value, 0.001f);
        }

        [Test]
        public void EnemySpawnerData_StoresAllFields()
        {
            var prefabEntity = _em.CreateEntity();
            var entity = _em.CreateEntity();
            _em.AddComponentData(entity, new EnemySpawnerData
            {
                Prefab = prefabEntity,
                Timer = 0.5f,
                Interval = 2.0f,
                SpawnMinX = -1.5f,
                SpawnMaxX = 1.5f,
                SpawnY = 4.0f
            });

            var data = _em.GetComponentData<EnemySpawnerData>(entity);
            Assert.AreEqual(prefabEntity, data.Prefab);
            Assert.AreEqual(0.5f, data.Timer, 0.001f);
            Assert.AreEqual(2.0f, data.Interval, 0.001f);
            Assert.AreEqual(-1.5f, data.SpawnMinX, 0.001f);
            Assert.AreEqual(1.5f, data.SpawnMaxX, 0.001f);
            Assert.AreEqual(4.0f, data.SpawnY, 0.001f);
        }
    }
}
