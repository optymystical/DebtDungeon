using DebtDungeon.Core;
using DebtDungeon.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The equipment station's screen. Near a station, a prompt says "E: Use station". Pressing E (out of combat)
    /// pauses the game through the GameManager and opens a small window: repair armor, and change gear with the
    /// same rows the character creator uses. E, Esc or Close shuts it and play resumes.
    /// Put it on the GameManager's object in Main, next to PlayerBootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public class StationScreen : MonoBehaviour
    {
        private const float WarningSeconds = 2f;

        [Tooltip("Leave empty to use the GameManager on this object.")]
        [SerializeField] private GameManager _gameManager;

        [Tooltip("Leave empty to use the PlayerBootstrap on this object.")]
        [SerializeField] private PlayerBootstrap _bootstrap;

        [Tooltip("Opens and closes the station while the player stands at one.")]
        [SerializeField] private InputAction _useAction = new InputAction("Use Station", InputActionType.Button, "<Keyboard>/e");

        private CharacterData _character;
        private PlayerDefenses _defenses;
        private ICombatState _combatState;
        private LoadoutEditor _loadoutEditor;
        private int _stationsInRange;
        private float _warningUntil;
        private bool _isOpen;

        private GameObject _promptRoot;
        private TextMeshProUGUI _promptText;
        private GameObject _window;
        private TextMeshProUGUI _armorText;
        private Button _repairButton;
        private RectTransform _loadoutList;
        private TextMeshProUGUI _burdenText;
        private TextMeshProUGUI _statusText;
        private Button _closeButton;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = GetComponent<GameManager>();
            }

            if (_bootstrap == null)
            {
                _bootstrap = GetComponent<PlayerBootstrap>();
            }

            Build();
            _window.SetActive(false);
            _promptRoot.SetActive(false);
        }

        private void Start()
        {
            if (_bootstrap == null || _bootstrap.Character == null)
            {
                Debug.LogError("StationScreen needs a PlayerBootstrap with a spawned character.", this);
                enabled = false;
                return;
            }

            _character = _bootstrap.Character;
            _defenses = _bootstrap.Defenses;
            _combatState = _bootstrap.PlayerObject != null ? _bootstrap.PlayerObject.GetComponent<ICombatState>() : null;

            ProgressionCatalog catalog = _bootstrap.Catalog;
            Archetype archetype = _character.Archetype;
            _loadoutEditor = new LoadoutEditor(_loadoutList, slot => catalog.GetEquipment(archetype, slot), SetStatus);

            _character.Loadout.LoadoutChanged += OnLoadoutChanged;
        }

        private void OnEnable()
        {
            EquipmentStation.PlayerRangeChanged += OnPlayerRangeChanged;
            GameEvents.GameStateChanged += OnGameStateChanged;
            _useAction.Enable();
        }

        private void OnDisable()
        {
            EquipmentStation.PlayerRangeChanged -= OnPlayerRangeChanged;
            GameEvents.GameStateChanged -= OnGameStateChanged;
            _useAction.Disable();
        }

        private void OnDestroy()
        {
            if (_character != null)
            {
                _character.Loadout.LoadoutChanged -= OnLoadoutChanged;
            }
        }

        private void Update()
        {
            if (_useAction.WasPressedThisFrame())
            {
                if (_isOpen)
                {
                    _gameManager.CloseStation();
                }
                else if (_stationsInRange > 0 && GameEvents.CurrentGameState == GameState.Playing)
                {
                    TryOpen();
                }
            }

            RefreshPrompt();
        }

        private void OnPlayerRangeChanged(bool inRange)
        {
            // A count, not a bool, in case two stations' ranges ever overlap.
            _stationsInRange = Mathf.Max(0, _stationsInRange + (inRange ? 1 : -1));
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            // Any way out of Paused (E, Esc, Close) ends up here, so this is the one place the window hides.
            if (_isOpen && current != GameState.Paused)
            {
                _isOpen = false;
                _window.SetActive(false);
            }
        }

        private void OnLoadoutChanged()
        {
            if (_isOpen)
            {
                Redraw();
            }
        }

        private void TryOpen()
        {
            if (_combatState != null && _combatState.InCombat)
            {
                // Uses unscaled time: the warning should time out the same way whatever Time.timeScale is.
                _warningUntil = Time.unscaledTime + WarningSeconds;
                return;
            }

            if (!_gameManager.TryOpenStation())
            {
                return;
            }

            _isOpen = true;
            _window.SetActive(true);
            SetStatus(string.Empty, false);
            Redraw();
            UIKit.Select(_closeButton);
        }

        private void Repair()
        {
            _defenses.RepairArmor();
            SetStatus("Armor repaired.", false);
            Redraw();
        }

        private void Redraw()
        {
            ArmorAbsorber armor = _defenses != null ? _defenses.Armor : null;
            if (armor == null)
            {
                _armorText.text = "No armor worn.";
                _repairButton.interactable = false;
            }
            else
            {
                _armorText.text = $"{armor.Armor.DisplayName}: {armor.CurrentDurability} / {armor.MaxDurability}";
                _repairButton.interactable = armor.CurrentDurability < armor.MaxDurability;
            }

            _loadoutEditor.Draw(_character.Loadout);
            _burdenText.text = LoadoutEditor.DescribeBurden(_character.Loadout);
        }

        private void RefreshPrompt()
        {
            bool warning = Time.unscaledTime < _warningUntil;
            bool show = !_isOpen && GameEvents.CurrentGameState == GameState.Playing && (_stationsInRange > 0 || warning);
            _promptRoot.SetActive(show);
            if (show)
            {
                _promptText.text = warning ? "You can't use the station while in combat." : "E: Use station";
                _promptText.color = warning ? UIKit.WarningText : UIKit.TextColor;
            }
        }

        private void SetStatus(string message, bool isWarning)
        {
            _statusText.text = message ?? string.Empty;
            _statusText.color = isWarning ? UIKit.WarningText : UIKit.MutedText;
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "Station Canvas", 60);

            // Prompt: a small strip near the bottom of the screen (inside the inner 90%).
            RectTransform prompt = UIKit.CreatePanel(canvas.transform, "Prompt", UIKit.PanelColor);
            prompt.anchorMin = new Vector2(0.5f, 0.12f);
            prompt.anchorMax = new Vector2(0.5f, 0.12f);
            prompt.sizeDelta = new Vector2(640f, 64f);
            _promptRoot = prompt.gameObject;
            _promptText = UIKit.CreateText(prompt, string.Empty, 28f, TextAlignmentOptions.Center, UIKit.TextColor);
            UIKit.Stretch(_promptText.rectTransform);

            // Window
            RectTransform backdrop = UIKit.CreatePanel(canvas.transform, "Backdrop", UIKit.Backdrop);
            UIKit.Stretch(backdrop);
            _window = backdrop.gameObject;

            RectTransform window = UIKit.CreatePanel(backdrop, "Window", UIKit.PanelColor);
            UIKit.Center(window, new Vector2(820f, 860f));
            UIKit.AddVerticalLayout(window, 12f, 32);

            UIKit.CreateText(window, "Equipment Station", 52f, TextAlignmentOptions.Center, UIKit.Accent);
            UIKit.CreateText(window, "Game paused", 22f, TextAlignmentOptions.Center, UIKit.MutedText);

            RectTransform armorRow = UIKit.CreateGroup(window, "Armor Row");
            UIKit.AddHorizontalLayout(armorRow, 12f, 0);
            UIKit.SetSize(armorRow, 60f);
            _armorText = UIKit.CreateText(armorRow, string.Empty, 26f, TextAlignmentOptions.MidlineLeft, UIKit.TextColor);
            _repairButton = UIKit.CreateButton(armorRow, "Repair armor", Repair, 60f);
            UIKit.SetSize(_repairButton, 60f, 220f).flexibleWidth = 0f;

            _loadoutList = UIKit.CreateGroup(window, "Loadout");
            UIKit.AddVerticalLayout(_loadoutList, 8f, 0);
            _burdenText = UIKit.CreateText(window, string.Empty, 26f, TextAlignmentOptions.Left, UIKit.Accent);
            _statusText = UIKit.CreateText(window, string.Empty, 22f, TextAlignmentOptions.Left, UIKit.MutedText);
            _closeButton = UIKit.CreateButton(window, "Close (E / Esc)", () => _gameManager.CloseStation(), 64f);
        }
    }
}
