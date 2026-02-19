using UnityEngine;
using Unity.Entities;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// 掛在場景中任意 GameObject 上，Bake 出 ScoreData singleton。
    /// 場景中應只有一個此元件。
    /// </summary>
    public class ScoreAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("遊戲開始時的初始分數")]
        private int _initialScore = 0;

        public class Baker : Baker<ScoreAuthoring>
        {
            public override void Bake(ScoreAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new Score.ScoreData
                {
                    Value = authoring._initialScore
                });
            }
        }
    }
}
