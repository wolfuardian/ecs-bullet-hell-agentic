using Unity.Entities;

namespace MyGame.ECS.Bullet
{
    /// <summary>
    /// 子彈剩餘存活時間（秒）。歸零時由 BulletLifetimeSystem 銷毀。
    /// </summary>
    public struct BulletLifetime : IComponentData
    {
        public float Value;
    }
}
