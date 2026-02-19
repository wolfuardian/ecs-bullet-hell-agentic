using UnityEngine;
using Unity.Entities;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 敵人生成器的 Authoring 元件。
    /// 放在場景中的 GameObject 上，定義敵人的生成頻率和位置範圍。
    /// Bake 產生 EnemySpawnerData singleton。
    /// </summary>
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        [Header("生成設定")]
        [SerializeField]
        [Tooltip("敵人 Prefab（需掛有 EnemyAuthoring）")]
        private GameObject _enemyPrefab;

        [SerializeField]
        [Tooltip("每隻敵人的生成間隔（秒）")]
        private float _spawnInterval = 2.0f;

        [Header("生成位置")]
        [SerializeField]
        [Tooltip("生成位置最小 X 座標")]
        private float _spawnMinX = -1.5f;

        [SerializeField]
        [Tooltip("生成位置最大 X 座標")]
        private float _spawnMaxX = 1.5f;

        [SerializeField]
        [Tooltip("生成位置 Y 座標（畫面上方）")]
        private float _spawnY = 4.0f;

        public class Baker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                Entity prefabEntity = Entity.Null;
                if (authoring._enemyPrefab != null)
                {
                    prefabEntity = GetEntity(
                        authoring._enemyPrefab, TransformUsageFlags.Dynamic);
                }

                AddComponent(entity, new Enemy.EnemySpawnerData
                {
                    Prefab = prefabEntity,
                    Timer = 0f,
                    Interval = authoring._spawnInterval,
                    SpawnMinX = authoring._spawnMinX,
                    SpawnMaxX = authoring._spawnMaxX,
                    SpawnY = authoring._spawnY
                });
            }
        }
    }
}
