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

        /// <summary>
        /// 在 Scene View 繪製邊界 Gizmo，方便開發者對齊相機視野。
        /// 綠色 = 玩家活動邊界，紅色 = 子彈銷毀邊界。
        /// </summary>
        private void OnDrawGizmos()
        {
            // 玩家邊界（綠色實線）
            Gizmos.color = Color.green;
            DrawRect(_playerMinX, _playerMaxX, _playerMinY, _playerMaxY);

            // 子彈銷毀邊界（紅色實線）
            Gizmos.color = Color.red;
            DrawRect(
                _playerMinX - _bulletMargin,
                _playerMaxX + _bulletMargin,
                _playerMinY - _bulletMargin,
                _playerMaxY + _bulletMargin);
        }

        private static void DrawRect(float minX, float maxX, float minY, float maxY)
        {
            var bl = new Vector3(minX, minY, 0f); // bottom-left
            var br = new Vector3(maxX, minY, 0f); // bottom-right
            var tr = new Vector3(maxX, maxY, 0f); // top-right
            var tl = new Vector3(minX, maxY, 0f); // top-left

            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }

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
