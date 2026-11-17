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

        private static readonly EquipmentSlot[] WornSlots =
            { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Foci, EquipmentSlot.Shroud };

        [SerializeField] private ProgressionCatalog _catalog;

        [Tooltip("Leave empty to use the GameManager on this object.")]
        [SerializeField] private GameManager _gameManager;

        private CharacterCreator _creator;
        private Loadout _watchedLoadout;

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
            UIKit.ClearChildren(_loadoutList);
            CharacterData draft = _creator.Draft;
            if (draft == null)
            {
                UIKit.CreateText(_loadoutList, "Pick a preset to edit its gear.", 24f, TextAlignmentOptions.Left, UIKit.MutedText);
                _burdenText.text = string.Empty;
                _startButton.interactable = false;
                return;
            }

            foreach (EquipmentSlot slot in WornSlots)
            {
                if (_creator.GetEquipmentChoices(slot).Count > 0)
                {
                    AddSlotRow(draft.Loadout, slot);
                }
            }

            List<EquipmentDefinition> affixes = _creator.GetEquipmentChoices(EquipmentSlot.Affix);
            if (affixes.Count > 0)
            {
                UIKit.CreateText(_loadoutList, "Affixes", 28f, TextAlignmentOptions.Left, UIKit.TextColor);
                foreach (EquipmentDefinition affix in affixes)
                {
                    AddAffixRow(draft.Loadout, (AffixDefinition)affix);
                }
            }

            Loadout loadout = draft.Loadout;
            _burdenText.text = $"Burden {loadout.BurdenSteps}/{Loadout.MaxBurdenSteps}    Move speed {Mathf.RoundToInt(loadout.MoveSpeedMultiplier * 100f)}%";
            _startButton.interactable = _creator.CanConfirm(out _);
        }

        // One row per slot: "< Sword >" cycles through this slot's items plus "(none)".
        private void AddSlotRow(Loadout loadout, EquipmentSlot slot)
        {
            EquipmentDefinition equipped = loadout.GetEquipped(slot);
            string itemName = equipped != null ? equipped.DisplayName : "(none)";

            RectTransform row = UIKit.CreateGroup(_loadoutList, slot + " Row");
            UIKit.AddHorizontalLayout(row, 8f, 0);
            UIKit.SetSize(row, 56f);

            Button previous = UIKit.CreateButton(row, "<", () => CycleSlot(loadout, slot, -1), 56f);
            UIKit.SetSize(previous, 56f, 56f).flexibleWidth = 0f;
            UIKit.CreateText(row, $"{slot}: {itemName}", 26f, TextAlignmentOptions.Center, UIKit.TextColor);
            Button next = UIKit.CreateButton(row, ">", () => CycleSlot(loadout, slot, 1), 56f);
            UIKit.SetSize(next, 56f, 56f).flexibleWidth = 0f;

            if (equipped != null && !string.IsNullOrEmpty(equipped.Description))
            {
                UIKit.CreateText(_loadoutList, equipped.Description, 20f, TextAlignmentOptions.Left, UIKit.MutedText);
            }
        }

        private void AddAffixRow(Loadout loadout, AffixDefinition affix)
        {
            string state = "not attuned";
            foreach (EquippedAffix attuned in loadout.Affixes)
            {
                if (attuned.Affix == affix)
                {
                    state = attuned.Form.ToString();
                }
            }

            RectTransform row = UIKit.CreateGroup(_loadoutList, affix.DisplayName + " Row");
            UIKit.AddHorizontalLayout(row, 8f, 0);
            UIKit.SetSize(row, 52f);

            UIKit.CreateText(row, $"{affix.DisplayName}: {state}", 24f, TextAlignmentOptions.MidlineLeft, UIKit.TextColor);
            UIKit.CreateButton(row, "Offensive", () => AttuneAffix(loadout, affix, AffixForm.Offensive), 52f);
            UIKit.CreateButton(row, "Defensive", () => AttuneAffix(loadout, affix, AffixForm.Defensive), 52f);
            UIKit.CreateButton(row, "Remove", () => RemoveAffix(loadout, affix), 52f);
        }

        // ----- Actions -----

        private void SelectPreset(PresetDefinition preset)
        {
            if (!_creator.TrySelectPreset(preset, out string reason))
            {
                SetStatus(reason, true);
            }
        }

        private void CycleSlot(Loadout loadout, EquipmentSlot slot, int direction)
        {
            // Choices are this slot's items plus null for "(none)"; step through them and wrap around.
            List<EquipmentDefinition> choices = new List<EquipmentDefinition> { null };
            choices.AddRange(_creator.GetEquipmentChoices(slot));

            int index = choices.IndexOf(loadout.GetEquipped(slot));
            int nextIndex = (index + direction + choices.Count) % choices.Count;
            EquipmentDefinition next = choices[nextIndex];

            string reason;
            bool changed = next == null ? loadout.TryUnequip(slot, out reason) : loadout.TryEquip(next, out reason);
            SetStatus(changed ? string.Empty : reason, !changed);
        }

        private void AttuneAffix(Loadout loadout, AffixDefinition affix, AffixForm form)
        {
            // Switching form = remove, then add. If the add is refused, put the old form back.
            AffixForm? oldForm = null;
            foreach (EquippedAffix attuned in loadout.Affixes)
            {
                if (attuned.Affix == affix)
                {
                    oldForm = attuned.Form;
                }
            }

            if (oldForm == form)
            {
                return;
            }

            if (oldForm.HasValue)
            {
                loadout.TryRemoveAffix(affix, out _);
            }

            if (loadout.TryAddAffix(affix, form, out string reason))
            {
                SetStatus(string.Empty, false);
                return;
            }

            if (oldForm.HasValue)
            {
                loadout.TryAddAffix(affix, oldForm.Value, out _);
            }

            SetStatus(reason, true);
        }

        private void RemoveAffix(Loadout loadout, AffixDefinition affix)
        {
            bool removed = loadout.TryRemoveAffix(affix, out string reason);
            SetStatus(removed ? string.Empty : reason, !removed);
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
