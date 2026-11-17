using System.Collections.Generic;
using DebtDungeon.Core;
using DebtDungeon.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The character creator screen: archetype -> preset -> loadout -> name -> Start. All rules live in
    /// Progression's CharacterCreator and Loadout; this class only draws them and passes clicks along.
    /// Start stores the blueprint in RunSession and asks the GameManager to load the Main scene.
    /// Put it on the GameManager's object in the MainMenu scene and assign the ProgressionCatalog.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterCreatorScreen : MonoBehaviour
    {
        // PlayerStats' default max HP formula, used only for the preset preview (50 + defensive stat x 5).
        private const int PreviewBaseHealth = 50;
        private const int PreviewHealthPerPoint = 5;
        private const int NameCharacterLimit = 24;

        [SerializeField] private ProgressionCatalog _catalog;

        [Tooltip("Leave empty to use the GameManager on this object.")]
        [SerializeField] private GameManager _gameManager;

        private CharacterCreator _creator;
        private Loadout _watchedLoadout;
        private LoadoutEditor _loadoutEditor;

        private GameObject _root;
        private RectTransform _archetypeList;
        private RectTransform _presetList;
        private RectTransform _loadoutList;
        private TextMeshProUGUI _burdenText;
        private TextMeshProUGUI _statusText;
        private TMP_InputField _nameBox;
        private Button _startButton;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = GetComponent<GameManager>();
            }

            if (_catalog == null)
            {
                Debug.LogError("CharacterCreatorScreen needs the ProgressionCatalog assigned.", this);
                enabled = false;
                return;
            }

            _creator = new CharacterCreator(_catalog);
            Build();
            _loadoutEditor = new LoadoutEditor(_loadoutList, _creator.GetEquipmentChoices, SetStatus);
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            if (_creator != null)
            {
                _creator.Changed += OnCreatorChanged;
            }
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            if (_creator != null)
            {
                _creator.Changed -= OnCreatorChanged;
            }

            WatchLoadout(null);
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            bool visible = current == GameState.CharacterCreation;
            _root.SetActive(visible);
            if (visible)
            {
                SetStatus(string.Empty, false);
                RefreshAll();
            }
        }

        // Archetype, preset or name changed. A new preset means a new draft, so watch its loadout instead.
        private void OnCreatorChanged()
        {
            WatchLoadout(_creator.Draft != null ? _creator.Draft.Loadout : null);
            RefreshAll();
        }

        private void OnLoadoutChanged()
        {
            RefreshLoadout();
        }

        private void WatchLoadout(Loadout loadout)
        {
            if (_watchedLoadout != null)
            {
                _watchedLoadout.LoadoutChanged -= OnLoadoutChanged;
            }

            _watchedLoadout = loadout;
            if (_watchedLoadout != null)
            {
                _watchedLoadout.LoadoutChanged += OnLoadoutChanged;
            }
        }

        // ----- Drawing -----

        private void RefreshAll()
        {
            RefreshArchetypes();
            RefreshPresets();
            RefreshLoadout();
        }

        private void RefreshArchetypes()
        {
            UIKit.ClearChildren(_archetypeList);
            foreach (Archetype archetype in _creator.GetArchetypeChoices())
            {
                Archetype choice = archetype; // copy for the click handler below
                bool selected = _creator.HasArchetype && _creator.Archetype == choice;
                UIKit.CreateButton(_archetypeList, Marked(choice.ToString(), selected), () => _creator.SelectArchetype(choice), 64f);
            }
        }

        private void RefreshPresets()
        {
            UIKit.ClearChildren(_presetList);
            if (!_creator.HasArchetype)
            {
                UIKit.CreateText(_presetList, "Pick an archetype first.", 24f, TextAlignmentOptions.Left, UIKit.MutedText);
                return;
            }

            foreach (PresetDefinition preset in _creator.GetPresetChoices())
            {
                PresetDefinition choice = preset;
                bool selected = _creator.Preset == choice;
                string label = Marked($"{choice.DisplayName}  ({choice.Role})", selected);
                UIKit.CreateButton(_presetList, label, () => SelectPreset(choice), 60f);
                UIKit.CreateText(_presetList, DescribePreset(choice), 22f, TextAlignmentOptions.Left, UIKit.MutedText);
            }
        }

        private void RefreshLoadout()
        {
            CharacterData draft = _creator.Draft;
            if (draft == null)
            {
                UIKit.ClearChildren(_loadoutList);
                UIKit.CreateText(_loadoutList, "Pick a preset to edit its gear.", 24f, TextAlignmentOptions.Left, UIKit.MutedText);
                _burdenText.text = string.Empty;
                _startButton.interactable = false;
                return;
            }

            // The rows themselves are drawn by LoadoutEditor, which the equipment station shares.
            _loadoutEditor.Draw(draft.Loadout);
            _burdenText.text = LoadoutEditor.DescribeBurden(draft.Loadout);
            _startButton.interactable = _creator.CanConfirm(out _);
        }

        // ----- Actions -----

        private void SelectPreset(PresetDefinition preset)
        {
            if (!_creator.TrySelectPreset(preset, out string reason))
            {
                SetStatus(reason, true);
            }
        }

        private void StartRun()
        {
            if (!_creator.TryConfirm(out CharacterBlueprint blueprint, out string reason))
            {
                SetStatus(reason, true);
                return;
            }

            RunSession.SetBlueprint(blueprint);
            _gameManager.StartRun();
        }

        // ----- Helpers -----

        private string DescribePreset(PresetDefinition preset)
        {
            CharacterStats stats = preset.CreateStats();
            List<string> parts = new List<string>();
            foreach (StatType stat in ArchetypeStats.GetStats(preset.Archetype))
            {
                parts.Add($"{stat} {stats.GetStat(stat)}");
            }

            int maxHealth = PlayerStats.CalculateMaxHealth(stats, PreviewBaseHealth, PreviewHealthPerPoint);
            string description = string.IsNullOrEmpty(preset.Description) ? string.Empty : preset.Description + "\n";
            return description + string.Join("   ", parts) + $"   HP {maxHealth}";
        }

        private static string Marked(string label, bool selected)
        {
            return selected ? $"> {label} <" : label;
        }

        private void SetStatus(string message, bool isWarning)
        {
            _statusText.text = message ?? string.Empty;
            _statusText.color = isWarning ? UIKit.WarningText : UIKit.MutedText;
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "Creator Canvas", 20);
            _root = canvas.gameObject;

            RectTransform background = UIKit.CreatePanel(canvas.transform, "Background", new Color(0.07f, 0.07f, 0.08f, 1f));
            UIKit.Stretch(background);
            UIKit.AddVerticalLayout(background, 20f, 48);

            UIKit.CreateText(background, "Create Your Debtor", 64f, TextAlignmentOptions.Center, UIKit.Accent);

            RectTransform columns = UIKit.CreateGroup(background, "Columns");
            UIKit.AddHorizontalLayout(columns, 32f, 0);
            UIKit.SetSize(columns, 820f);

            RectTransform left = CreateColumn(columns, "1. Archetype");
            _archetypeList = CreateList(left, "Archetypes");
            UIKit.CreateText(left, "2. Preset", 32f, TextAlignmentOptions.Left, UIKit.TextColor);
            _presetList = CreateList(left, "Presets");

            RectTransform middle = CreateColumn(columns, "3. Loadout");
            _loadoutList = CreateList(middle, "Loadout");
            _burdenText = UIKit.CreateText(middle, string.Empty, 26f, TextAlignmentOptions.Left, UIKit.Accent);
            UIKit.CreateText(middle, "You can change gear and repair armor later at the station in your starting room (out of combat).", 20f, TextAlignmentOptions.Left, UIKit.MutedText);

            RectTransform right = CreateColumn(columns, "4. Name");
            _nameBox = UIInput.CreateTextBox(right, "Unnamed Debtor", NameCharacterLimit, 64f);
            _nameBox.onValueChanged.AddListener(_creator.SetName);
            _startButton = UIKit.CreateButton(right, "Start Run", StartRun, 72f);
            UIKit.CreateButton(right, "Back", _gameManager.BackToTitle, 64f);
            _statusText = UIKit.CreateText(right, string.Empty, 24f, TextAlignmentOptions.Left, UIKit.MutedText);
        }

        private static RectTransform CreateColumn(Transform parent, string heading)
        {
            RectTransform column = UIKit.CreatePanel(parent, heading, UIKit.PanelColor);
            UIKit.AddVerticalLayout(column, 12f, 24);
            UIKit.CreateText(column, heading, 32f, TextAlignmentOptions.Left, UIKit.TextColor);
            return column;
        }

        private static RectTransform CreateList(Transform parent, string name)
        {
            RectTransform list = UIKit.CreateGroup(parent, name);
            UIKit.AddVerticalLayout(list, 8f, 0);
            return list;
        }
    }
}
