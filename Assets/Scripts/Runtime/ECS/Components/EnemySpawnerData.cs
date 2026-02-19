using Unity.Entities;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// Singleton — 敵人生成器資料。控制敵人的週期性生成。
    /// </summary>
    public struct EnemySpawnerData : IComponentData
    {
        /// <summary>敵人 Prefab Entity。</summary>
        public Entity Prefab;

        /// <summary>目前生成計時器，歸零時生成一隻敵人。</summary>
        public float Timer;

        /// <summary>每次生成後重置的間隔時長（秒）。</summary>
        public float Interval;

        /// <summary>生成位置的最小 X 座標。</summary>
        public float SpawnMinX;

        /// <summary>生成位置的最大 X 座標。</summary>
        public float SpawnMaxX;

        /// <summary>生成位置的 Y 座標（畫面上方）。</summary>
        public float SpawnY;
    }
}
