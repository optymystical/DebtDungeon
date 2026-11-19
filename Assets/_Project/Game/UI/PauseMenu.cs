using DebtDungeon.Core;
using TMPro;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The pause screen: shown while the game is Paused (Esc, handled by the GameManager), with Resume and Main Menu.
    /// Builds its own UI in Awake. Put it on the GameManager's object in the Main scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("Leave empty to use the GameManager on this object.")]
        [SerializeField] private GameManager _gameManager;

        private GameObject _root;
        private UnityEngine.UI.Button _resumeButton;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = GetComponent<GameManager>();
            }

            Build();
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            // The station also pauses the game, but shows its own screen instead of this one.
            bool visible = current == GameState.Paused && !_gameManager.IsStationOpen;
            _root.SetActive(visible);
            if (visible)
            {
                UIKit.Select(_resumeButton);
            }
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "Pause Canvas", 100);
            _root = canvas.gameObject;

            RectTransform backdrop = UIKit.CreatePanel(canvas.transform, "Backdrop", UIKit.Backdrop);
            UIKit.Stretch(backdrop);

            RectTransform window = UIKit.CreatePanel(backdrop, "Window", UIKit.PanelColor);
            UIKit.Center(window, new Vector2(560f, 420f));
            UIKit.AddVerticalLayout(window, 20f, 40);

            UIKit.CreateText(window, "Paused", 64f, TextAlignmentOptions.Center, UIKit.TextColor);
            _resumeButton = UIKit.CreateButton(window, "Resume", _gameManager.TogglePause, 64f);
            UIKit.CreateButton(window, "Main Menu", _gameManager.QuitToMainMenu, 64f);
            UIKit.CreateText(window, "Esc to resume", 24f, TextAlignmentOptions.Center, UIKit.MutedText);
        }
    }
}
