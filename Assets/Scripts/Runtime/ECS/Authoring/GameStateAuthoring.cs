using UnityEngine;
using Unity.Entities;

namespace MyGame.ECS.Authoring
{
    /// <summary>
    /// Bakes a GameStateData singleton into the ECS world.
    /// Place on a single GameObject in the scene.
    /// </summary>
    public class GameStateAuthoring : MonoBehaviour
    {
        public class Baker : Baker<GameStateAuthoring>
        {
            public override void Bake(GameStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new GameState.GameStateData
                {
                    State = GameState.GameStateData.PLAYING
                });
            }
        }
    }
}
