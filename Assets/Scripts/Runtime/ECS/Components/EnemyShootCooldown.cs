using Unity.Entities;

namespace MyGame.ECS.Enemy
{
    /// <summary>
    /// 敵人射擊冷卻 Component。Timer 逐幀遞減，歸零時發射子彈。
    /// </summary>
    public struct EnemyShootCooldown : IComponentData
    {
        /// <summary>目前冷卻計時器，歸零代表可射擊。</summary>
        public float Timer;

        /// <summary>每次射擊後重置的冷卻時長。</summary>
        public float Duration;
    }
}
