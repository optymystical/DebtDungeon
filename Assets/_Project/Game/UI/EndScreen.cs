using DebtDungeon.Core;
using TMPro;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The death screen and the results screen (same layout): what happened, the run's stats, Retry and Main Menu.
    /// Shown when the game enters Dead or Won. Put it on the GameManager's object in the Main scene, next to a
    /// RunStatsTracker.
    /// </summary>
    [DisallowMultipleComponent]
    public class EndScreen : MonoBehaviour
    {
        [Tooltip("Leave empty to use the GameManager on this object.")]
        [SerializeField] private GameManager _gameManager;

        [Tooltip("Leave empty to use the RunStatsTracker on this object.")]
        [SerializeField] private RunStatsTracker _stats;

        private GameObject _root;
        private KillInfo _death;
        private bool _hasDeath;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _subtitle;
        private TextMeshProUGUI _statsText;
        private UnityEngine.UI.Button _retryButton;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = GetComponent<GameManager>();
            }

            if (_stats == null)
            {
                _stats = GetComponent<RunStatsTracker>();
            }

            Build();
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            GameEvents.PlayerDied -= OnPlayerDied;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            bool isEnd = current == GameState.Dead || current == GameState.Won;
            _root.SetActive(isEnd);
            if (isEnd)
            {
                Refresh(current);
                UIKit.Select(_retryButton);
            }
        }

        // PlayerDied and the switch to Dead happen in the same frame, and Unity doesn't promise which listener
        // runs first (RunStatsTracker may not have stored the kill yet). So keep our own copy of the kill from the event.
        private void OnPlayerDied(KillInfo kill)
        {
            _death = kill;
            _hasDeath = true;
            Refresh(GameState.Dead);
        }

        private void Refresh(GameState state)
        {
            bool died = state == GameState.Dead;
            _title.text = died ? "You Died" : "You Escaped!";
            _title.color = died ? UIKit.WarningText : UIKit.Accent;
            _subtitle.text = died ? DescribeDeath() : "You destroyed a camp and reached the exit. The collectors will want their cut.";

            if (_stats == null)
            {
                _statsText.text = string.Empty;
                return;
            }

            _statsText.text =
                $"Time  {RunStatsTracker.FormatTime(_stats.ElapsedSeconds)}     Level  {_stats.Level}     Gold  {_stats.Gold}\n" +
                $"Goblins slain  {_stats.GetKills(Faction.Goblin)}     Spiders slain  {_stats.GetKills(Faction.Spider)}";
        }

        private string DescribeDeath()
        {
            if (!_hasDeath || string.IsNullOrEmpty(_death.KillerName))
            {
                return "You were slain.";
            }

            return $"Slain by {CleanName(_death.KillerName)}";
        }

        // A killer without an IKillReward name falls back to its GameObject name, e.g. "GoblinGrunt(Clone)".
        private static string CleanName(string name)
        {
            return name.Replace("(Clone)", string.Empty).Trim();
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "End Screen Canvas", 110);
            _root = canvas.gameObject;

            RectTransform backdrop = UIKit.CreatePanel(canvas.transform, "Backdrop", UIKit.Backdrop);
            UIKit.Stretch(backdrop);

            RectTransform window = UIKit.CreatePanel(backdrop, "Window", UIKit.PanelColor);
            UIKit.Center(window, new Vector2(820f, 560f));
            UIKit.AddVerticalLayout(window, 18f, 40);

            _title = UIKit.CreateText(window, string.Empty, 72f, TextAlignmentOptions.Center, UIKit.TextColor);
            _subtitle = UIKit.CreateText(window, string.Empty, 32f, TextAlignmentOptions.Center, UIKit.MutedText);
            _statsText = UIKit.CreateText(window, string.Empty, 28f, TextAlignmentOptions.Center, UIKit.TextColor);
            _retryButton = UIKit.CreateButton(window, "Retry", _gameManager.Retry, 64f);
            UIKit.CreateButton(window, "Main Menu", _gameManager.QuitToMainMenu, 64f);
        }
    }
}
