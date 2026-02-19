using Unity.Entities;

namespace MyGame.ECS.Player
{
    /// <summary>
    /// 射擊冷卻 Component。Timer 逐幀遞減，歸零時可射擊。
    /// </summary>
    public struct ShootCooldown : IComponentData
    {
        /// <summary>目前冷卻計時器，歸零代表可射擊。</summary>
        public float Timer;

        /// <summary>每次射擊後重置的冷卻時長。</summary>
        public float Duration;
    }
}
