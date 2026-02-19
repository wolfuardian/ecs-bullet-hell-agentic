using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using MyGame.ECS.Score;
using MyGame.ECS.Graze;
using MyGame.ECS.Bomb;
using MyGame.ECS.Collision;
using MyGame.ECS.Player;

namespace MyGame.HUD
{
    /// <summary>
    /// 從 ECS World 讀取遊戲狀態，顯示於 Legacy UI Text。
    /// 掛在 Canvas 底下的 GameObject 上，並拖入對應的 Text 元件。
    /// </summary>
    public class HUDDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("顯示分數的 Text 元件")]
        private Text _scoreText;

        [SerializeField]
        [Tooltip("顯示擦彈數的 Text 元件")]
        private Text _grazeText;

        [SerializeField]
        [Tooltip("顯示炸彈庫存的 Text 元件")]
        private Text _bombText;

        [SerializeField]
        [Tooltip("顯示玩家 HP 的 Text 元件")]
        private Text _hpText;

        private EntityManager _em;
        private bool _worldReady;

        private void LateUpdate()
        {
            if (!TryGetEntityManager())
                return;

            UpdateScoreText();
            UpdateGrazeText();
            UpdateBombText();
            UpdateHPText();
        }

        private bool TryGetEntityManager()
        {
            if (_worldReady && World.DefaultGameObjectInjectionWorld != null)
                return true;

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                _worldReady = false;
                return false;
            }

            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _worldReady = true;
            return true;
        }

        private void UpdateScoreText()
        {
            if (_scoreText == null)
                return;

            var query = _em.CreateEntityQuery(typeof(ScoreData));
            if (query.IsEmpty)
            {
                _scoreText.text = "Score: 0";
                return;
            }

            var score = query.GetSingleton<ScoreData>();
            _scoreText.text = $"Score: {score.Value}";
        }

        private void UpdateGrazeText()
        {
            if (_grazeText == null)
                return;

            var query = _em.CreateEntityQuery(typeof(GrazeData), typeof(PlayerTag));
            if (query.IsEmpty)
            {
                _grazeText.text = "Graze: 0";
                return;
            }

            var graze = query.GetSingleton<GrazeData>();
            _grazeText.text = $"Graze: {graze.Count}";
        }

        private void UpdateBombText()
        {
            if (_bombText == null)
                return;

            var query = _em.CreateEntityQuery(typeof(BombData), typeof(PlayerTag));
            if (query.IsEmpty)
            {
                _bombText.text = "Bomb: 0";
                return;
            }

            var bomb = query.GetSingleton<BombData>();
            _bombText.text = $"Bomb: {bomb.Stock}";
        }

        private void UpdateHPText()
        {
            if (_hpText == null)
                return;

            var query = _em.CreateEntityQuery(typeof(HealthData), typeof(PlayerTag));
            if (query.IsEmpty)
            {
                _hpText.text = "HP: 0";
                return;
            }

            var health = query.GetSingleton<HealthData>();
            _hpText.text = $"HP: {health.Current}/{health.Max}";
        }
    }
}
