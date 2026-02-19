using UnityEngine;
using Unity.Entities;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// Difficulty scaling Authoring component.
    /// Place on a GameObject in the scene to configure difficulty progression.
    /// Bakes a DifficultyData singleton.
    /// </summary>
    public class DifficultyAuthoring : MonoBehaviour
    {
        [Header("Difficulty Scaling")]
        [SerializeField]
        [Tooltip("Maximum spawn rate multiplier")]
        private float _maxMultiplier = 3.0f;

        [SerializeField]
        [Tooltip("Seconds per 1x multiplier increase")]
        private float _scalingInterval = 30f;

        [SerializeField]
        [Tooltip("Base enemy spawn interval (snapshot)")]
        private float _baseSpawnInterval = 2.0f;

        public class Baker : Baker<DifficultyAuthoring>
        {
            public override void Bake(DifficultyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new Difficulty.DifficultyData
                {
                    ElapsedTime = 0f,
                    SpawnRateMultiplier = 1f,
                    MaxMultiplier = authoring._maxMultiplier,
                    ScalingInterval = authoring._scalingInterval,
                    BaseSpawnInterval = authoring._baseSpawnInterval
                });
            }
        }
    }
}
