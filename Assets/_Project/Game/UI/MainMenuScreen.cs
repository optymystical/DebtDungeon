using DebtDungeon.Core;
using TMPro;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The title screen: Start (to character creation), Options (master, music and SFX volume, fullscreen), Credits and Quit.
    /// Shown while the game is in MainMenu. Put it on the GameManager's object in the MainMenu scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuScreen : MonoBehaviour
    {
        [Tooltip("Leave empty to use the GameManager on this object.")]
        [SerializeField] private GameManager _gameManager;

        private GameObject _root;
        private GameObject _credits;
        private GameObject _options;
        private UnityEngine.UI.Button _startButton;
        private UnityEngine.UI.Button _optionsButton;
        private UnityEngine.UI.Button _creditsBackButton;
        private UnityEngine.UI.Button _optionsBackButton;
        private UnityEngine.UI.Button _fullscreenButton;
        private TextMeshProUGUI _volumeText;
        private TextMeshProUGUI _musicText;
        private TextMeshProUGUI _sfxText;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = GetComponent<GameManager>();
            }

            Build();
            _credits.SetActive(false);
            _options.SetActive(false);
            Show(GameEvents.CurrentGameState == GameState.MainMenu);
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
            Show(current == GameState.MainMenu);
        }

        private void Show(bool visible)
        {
            _root.SetActive(visible);
            if (visible)
            {
                _credits.SetActive(false);
                _options.SetActive(false);
                UIKit.Select(_startButton);
            }
        }

        private void ShowCredits(bool visible)
        {
            _credits.SetActive(visible);
            UIKit.Select(visible ? _creditsBackButton : _startButton);
        }

        private void ShowOptions(bool visible)
        {
            _options.SetActive(visible);
            if (visible)
            {
                RefreshOptions();
            }

            UIKit.Select(visible ? _optionsBackButton : _optionsButton);
        }

        private void ChangeVolume(float step)
        {
            GameSettings.SetMasterVolume(GameSettings.MasterVolume + step);
            RefreshOptions();
        }

        private void ChangeMusic(float step)
        {
            GameSettings.SetMusicVolume(GameSettings.MusicVolume + step);
            RefreshOptions();
        }

        private void ChangeSfx(float step)
        {
            GameSettings.SetSfxVolume(GameSettings.SfxVolume + step);
            RefreshOptions();
        }

        private void ToggleFullscreen()
        {
            GameSettings.SetFullscreen(!GameSettings.Fullscreen);
            RefreshOptions();
        }

        private void RefreshOptions()
        {
            _volumeText.text = $"Master  {Percent(GameSettings.MasterVolume)}";
            _musicText.text = $"Music  {Percent(GameSettings.MusicVolume)}";
            _sfxText.text = $"Sound effects  {Percent(GameSettings.SfxVolume)}";
            UIKit.SetButtonLabel(_fullscreenButton, GameSettings.Fullscreen ? "Fullscreen: On" : "Fullscreen: Off");
        }

        private static string Percent(float volume)
        {
            return Mathf.RoundToInt(volume * 100f) + "%";
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "Main Menu Canvas", 10);
            _root = canvas.gameObject;

            RectTransform background = UIKit.CreatePanel(canvas.transform, "Background", new Color(0.07f, 0.07f, 0.08f, 1f));
            UIKit.Stretch(background);

            RectTransform column = UIKit.CreateGroup(background, "Column");
            // Wide enough for the tagline on one line and tall enough for everything, so the layout never has
            // to squeeze a label below its real height (that's what made the tagline overlap Start).
            UIKit.Center(column, new Vector2(960f, 760f));
            UIKit.AddVerticalLayout(column, 22f, 0);

            UIKit.CreateText(column, "Debt Dungeon", 96f, TextAlignmentOptions.Center, UIKit.Accent);
            UIKit.CreateText(column, "Work off what you owe. Try not to die doing it.", 28f, TextAlignmentOptions.Center, UIKit.MutedText);
            // The buttons sit in their own group, padded in from the sides so they stay 560 px wide.
            RectTransform buttons = UIKit.CreateGroup(column, "Buttons");
            UIKit.AddVerticalLayout(buttons, 22f, 0).padding = new RectOffset(200, 200, 0, 0);
            _startButton = UIKit.CreateButton(buttons, "Start", _gameManager.OpenCharacterCreation, 72f);
            _optionsButton = UIKit.CreateButton(buttons, "Options", () => ShowOptions(true), 72f);
            UIKit.CreateButton(buttons, "Credits", () => ShowCredits(true), 72f);
            UIKit.CreateButton(buttons, "Quit", Quit, 72f);

            // Credits: a window over the menu
            RectTransform credits = UIKit.CreatePanel(background, "Credits", UIKit.Backdrop);
            UIKit.Stretch(credits);
            _credits = credits.gameObject;
            RectTransform window = UIKit.CreatePanel(credits, "Window", UIKit.PanelColor);
            UIKit.Center(window, new Vector2(720f, 560f));
            UIKit.AddVerticalLayout(window, 16f, 40);
            UIKit.CreateText(window, "Debt Collectors", 56f, TextAlignmentOptions.Center, UIKit.Accent);
            UIKit.CreateText(window,
                "Christopher Caplinger: Progression, Core, Game (team lead)\n" +
                "Gregory Rooney: Floor & Levels\n" +
                "Larry Miller: Faction AI\n" +
                "Matthew Phillips: Player & Combat\n\n" +
                "Skill trees adapted from Flow State. Full Sail University, 2026.",
                26f, TextAlignmentOptions.Center, UIKit.TextColor);
            _creditsBackButton = UIKit.CreateButton(window, "Back", () => ShowCredits(false), 64f);

            // Options: same kind of window. Volume uses - / + buttons (like the creator's < / >) so the keyboard works.
            RectTransform options = UIKit.CreatePanel(background, "Options", UIKit.Backdrop);
            UIKit.Stretch(options);
            _options = options.gameObject;
            RectTransform optionsWindow = UIKit.CreatePanel(options, "Window", UIKit.PanelColor);
            UIKit.Center(optionsWindow, new Vector2(680f, 620f));
            UIKit.AddVerticalLayout(optionsWindow, 16f, 40);
            UIKit.CreateText(optionsWindow, "Options", 56f, TextAlignmentOptions.Center, UIKit.Accent);

            _volumeText = CreateVolumeRow(optionsWindow, "Master", ChangeVolume);
            _musicText = CreateVolumeRow(optionsWindow, "Music", ChangeMusic);
            _sfxText = CreateVolumeRow(optionsWindow, "Sound effects", ChangeSfx);

            _fullscreenButton = UIKit.CreateButton(optionsWindow, string.Empty, ToggleFullscreen, 64f);
            UIKit.CreateText(optionsWindow, "More options in Beta.", 22f, TextAlignmentOptions.Center, UIKit.MutedText);
            _optionsBackButton = UIKit.CreateButton(optionsWindow, "Back", () => ShowOptions(false), 64f);
        }

        // One "-  Music 60%  +" row. changeBy is called with -0.1 or +0.1.
        private static TextMeshProUGUI CreateVolumeRow(Transform parent, string name, System.Action<float> changeBy)
        {
            RectTransform row = UIKit.CreateGroup(parent, name + " Row");
            UIKit.AddHorizontalLayout(row, 12f, 0);
            UIKit.SetSize(row, 64f);
            UIKit.SetSize(UIKit.CreateButton(row, "-", () => changeBy(-0.1f), 64f), 64f, 80f).flexibleWidth = 0f;
            TextMeshProUGUI text = UIKit.CreateText(row, string.Empty, 30f, TextAlignmentOptions.Center, UIKit.TextColor);
            UIKit.SetSize(UIKit.CreateButton(row, "+", () => changeBy(0.1f), 64f), 64f, 80f).flexibleWidth = 0f;
            return text;
        }
    }
}
