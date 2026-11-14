using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Runs the game flow in one scene. Owns a GameStateMachine and applies what each state means:
    /// announces it through GameEvents, freezes or unfreezes time, and loads scenes for Start, Retry and Main Menu.
    /// One per scene (MainMenu and Main), with its Start State set to match. No DontDestroyOnLoad and no singleton:
    /// the last announced state carries across scene loads in GameEvents.CurrentGameState.
    /// UI buttons call the public methods.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        /// <summary>Build index of the MainMenu scene (title and character creation).</summary>
        public const int MainMenuSceneIndex = 0;

        /// <summary>Build index of the Main scene (the dungeon run).</summary>
        public const int MainSceneIndex = 1;

        [Tooltip("MainMenu in the MainMenu scene, Playing in the Main scene.")]
        [SerializeField] private GameState _startState = GameState.MainMenu;

        [Tooltip("Toggles pause while Playing or Paused. Its own action, so it works whatever action maps the player has enabled.")]
        [SerializeField] private InputAction _pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");

        private GameStateMachine _machine;

        /// <summary>The state the game is in now.</summary>
        public GameState CurrentState => _machine.Current;

        private void Awake()
        {
            _machine = new GameStateMachine(_startState);
        }

        private void OnEnable()
        {
            _machine.StateChanged += OnStateChanged;
            GameEvents.PlayerDied += OnPlayerDied;
            _pauseAction.Enable();
        }

        private void Start()
        {
            // Announce this scene's starting state. The previous state is whatever the last scene announced.
            // Listeners in this scene subscribed in OnEnable, which runs before Start, so they all hear it.
            ApplyTimeScale(_machine.Current);
            GameEvents.RaiseGameStateChanged(GameEvents.CurrentGameState, _machine.Current);
        }

        private void Update()
        {
            if (_pauseAction.WasPressedThisFrame())
            {
                TogglePause();
            }
        }

        private void OnDisable()
        {
            _machine.StateChanged -= OnStateChanged;
            GameEvents.PlayerDied -= OnPlayerDied;
            _pauseAction.Disable();
        }

        /// <summary>Main menu's Start button: go to character creation (same scene).</summary>
        public void OpenCharacterCreation()
        {
            TryChange(GameState.CharacterCreation);
        }

        /// <summary>Creator's Back button: return to the title (same scene).</summary>
        public void BackToTitle()
        {
            TryChange(GameState.MainMenu);
        }

        /// <summary>Creator's Confirm button: start the run by loading the Main scene.</summary>
        public void StartRun()
        {
            if (TryChange(GameState.Playing))
            {
                SceneManager.LoadScene(MainSceneIndex);
            }
        }

        /// <summary>Pause if playing, resume if paused. Any other state ignores it.</summary>
        [ContextMenu("Toggle Pause")]
        public void TogglePause()
        {
            if (_machine.Current == GameState.Playing)
            {
                TryChange(GameState.Paused);
            }
            else if (_machine.Current == GameState.Paused)
            {
                TryChange(GameState.Playing);
            }
        }

        /// <summary>Called by the exit when the player walks into it.</summary>
        [ContextMenu("Report Exit Reached")]
        public void ReportExitReached()
        {
            TryChange(GameState.Won);
        }

        /// <summary>Death or results screen's Retry button: a fresh run, by reloading the Main scene.</summary>
        [ContextMenu("Retry")]
        public void Retry()
        {
            if (_machine.Current != GameState.Dead && _machine.Current != GameState.Won)
            {
                return;
            }

            if (TryChange(GameState.Playing))
            {
                SceneManager.LoadScene(MainSceneIndex);
            }
        }

        /// <summary>Pause, death or results screen's Main Menu button.</summary>
        [ContextMenu("Quit To Main Menu")]
        public void QuitToMainMenu()
        {
            if (TryChange(GameState.MainMenu))
            {
                SceneManager.LoadScene(MainMenuSceneIndex);
            }
        }

        private bool TryChange(GameState next)
        {
            if (_machine.TryChangeState(next))
            {
                return true;
            }

            Debug.LogWarning($"Game flow: can't go from {_machine.Current} to {next}.", this);
            return false;
        }

        private void OnStateChanged(GameState previous, GameState current)
        {
            ApplyTimeScale(current);
            GameEvents.RaiseGameStateChanged(previous, current);
        }

        private void OnPlayerDied(KillInfo kill)
        {
            TryChange(GameState.Dead);
        }

        private static void ApplyTimeScale(GameState state)
        {
            Time.timeScale = GameStateMachine.IsTimeFrozen(state) ? 0f : 1f;
        }
    }
}
