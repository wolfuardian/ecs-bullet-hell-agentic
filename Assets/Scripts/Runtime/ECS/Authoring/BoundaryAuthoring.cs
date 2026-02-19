using UnityEngine;
using Unity.Entities;
using MyGame.ECS.Boundary;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 遊戲場地邊界的 Authoring 元件。
    /// 放在場景中的 GameObject 上，定義玩家的活動範圍和子彈的銷毀範圍。
    /// Bake 產生兩個 Singleton：PlayerBoundaryData 和 BulletBoundaryData。
    /// </summary>
    public class BoundaryAuthoring : MonoBehaviour
    {
        [Header("Play Area (Player Clamp)")]
        [Tooltip("玩家最小 X 座標")]
        [SerializeField] private float _playerMinX = -2.0f;

        [Tooltip("玩家最大 X 座標")]
        [SerializeField] private float _playerMaxX = 2.0f;

        [Tooltip("玩家最小 Y 座標")]
        [SerializeField] private float _playerMinY = -3.0f;

        [Tooltip("玩家最大 Y 座標")]
        [SerializeField] private float _playerMaxY = 3.0f;

        [Header("Bullet Destruction Margin")]
        [Tooltip("超過玩家邊界多少距離後銷毀子彈（預設 2.0）")]
        [SerializeField] private float _bulletMargin = 2.0f;

        public class Baker : Baker<BoundaryAuthoring>
        {
            public override void Bake(BoundaryAuthoring authoring)
            {
                // 純資料 singleton，不需要 Transform
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new PlayerBoundaryData
                {
                    MinX = authoring._playerMinX,
                    MaxX = authoring._playerMaxX,
                    MinY = authoring._playerMinY,
                    MaxY = authoring._playerMaxY
                });

                AddComponent(entity, new BulletBoundaryData
                {
                    MinX = authoring._playerMinX - authoring._bulletMargin,
                    MaxX = authoring._playerMaxX + authoring._bulletMargin,
                    MinY = authoring._playerMinY - authoring._bulletMargin,
                    MaxY = authoring._playerMaxY + authoring._bulletMargin
                });
            }
        }
    }
}
