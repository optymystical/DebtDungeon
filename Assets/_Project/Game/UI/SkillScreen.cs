using System.Collections.Generic;
using DebtDungeon.Core;
using DebtDungeon.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The character screen (Tab): spend stat points, buy skill tree nodes, see owned abilities and worn gear.
    /// All rules live in Progression (CharacterData, CharacterSkills); this class only draws them and passes
    /// clicks along, so refusals (in combat, no points, wrong archetype) come back as text from the same rules.
    /// It doesn't pause the game. Put it on the GameManager's object in the Main scene, next to PlayerBootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillScreen : MonoBehaviour
    {
        private static readonly EquipmentSlot[] WornSlots =
            { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Foci, EquipmentSlot.Shroud };

        [Tooltip("Leave empty to use the PlayerBootstrap on this object.")]
        [SerializeField] private PlayerBootstrap _bootstrap;

        [Tooltip("Opens and closes the screen while playing.")]
        [SerializeField] private InputAction _toggleAction = new InputAction("Skills", InputActionType.Button, "<Keyboard>/tab");

        private CharacterData _character;
        private ICombatState _combatState;
        private List<SkillTreeDefinition> _trees = new List<SkillTreeDefinition>();
        private bool _isDirty;

        private GameObject _root;
        private RectTransform _statsList;
        private RectTransform _treeRow;
        private RectTransform _ownedList;
        private TextMeshProUGUI _detailsText;
        private TextMeshProUGUI _statusText;

        private void Awake()
        {
            if (_bootstrap == null)
            {
                _bootstrap = GetComponent<PlayerBootstrap>();
            }

            Build();
            _root.SetActive(false);
        }

        private void Start()
        {
            if (_bootstrap == null || _bootstrap.Character == null)
            {
                Debug.LogError("SkillScreen needs a PlayerBootstrap with a spawned character.", this);
                enabled = false;
                return;
            }

            _character = _bootstrap.Character;
            _trees = _bootstrap.Catalog.GetSkillTrees(_character.Archetype);

            // Null until Player's In-combat flag exists; null means "not in combat", so spending is allowed.
            _combatState = _bootstrap.PlayerObject != null ? _bootstrap.PlayerObject.GetComponent<ICombatState>() : null;

            _character.Progress.ProgressChanged += MarkDirty;
            _character.Stats.StatsChanged += MarkDirty;
            _character.Skills.SkillsChanged += MarkDirty;
            _character.Loadout.LoadoutChanged += MarkDirty;
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
            UITooltipTarget.HoverChanged += OnHoverChanged;
            _toggleAction.Enable();
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
            UITooltipTarget.HoverChanged -= OnHoverChanged;
            _toggleAction.Disable();
        }

        private void OnDestroy()
        {
            if (_character == null)
            {
                return;
            }

            _character.Progress.ProgressChanged -= MarkDirty;
            _character.Stats.StatsChanged -= MarkDirty;
            _character.Skills.SkillsChanged -= MarkDirty;
            _character.Loadout.LoadoutChanged -= MarkDirty;
        }

        private void Update()
        {
            if (_toggleAction.WasPressedThisFrame() && GameEvents.CurrentGameState == GameState.Playing)
            {
                SetOpen(!_root.activeSelf);
            }

            // Several events can fire in one frame (e.g. a level-up changes points and stats); redraw once.
            if (_isDirty && _root.activeSelf)
            {
                _isDirty = false;
                Redraw();
            }
        }

        private void OnGameStateChanged(GameState previous, GameState current)
        {
            if (current != GameState.Playing)
            {
                SetOpen(false);
            }
        }

        private void SetOpen(bool open)
        {
            _root.SetActive(open);
            if (open)
            {
                _statusText.text = string.Empty;
                _detailsText.text = "Hover a stat, node or ability for details.";
                Redraw();
            }
        }

        private void MarkDirty()
        {
            _isDirty = true;
        }

        private void OnHoverChanged(string text)
        {
            if (_root.activeSelf && !string.IsNullOrEmpty(text))
            {
                _detailsText.text = text;
            }
        }

        // ----- Drawing -----

        private void Redraw()
        {
            DrawStats();
            DrawTrees();
            DrawOwned();
        }

        private void DrawStats()
        {
            UIKit.ClearChildren(_statsList);
            CharacterProgress progress = _character.Progress;

            string xp = progress.IsMaxLevel ? "MAX" : $"{progress.CurrentXp} / {progress.XpToNextLevel}";
            UIKit.CreateText(_statsList, $"{_character.Name}\nLevel {progress.Level}   XP {xp}", 28f, TextAlignmentOptions.Left, UIKit.TextColor);
            UIKit.CreateText(_statsList, $"Stat points: {progress.UnspentStatPoints}", 26f, TextAlignmentOptions.Left, UIKit.Accent);

            foreach (StatType stat in ArchetypeStats.GetStats(_character.Archetype))
            {
                StatType choice = stat; // copy for the click handler
                int value = _character.Stats.GetStat(choice);
                float power = StatRules.GetPower(value);

                RectTransform row = UIKit.CreateGroup(_statsList, choice + " Row");
                UIKit.AddHorizontalLayout(row, 8f, 0);
                UIKit.SetSize(row, 52f);
                // Name the worn gear that scales with this stat, so it's clear which stats matter right now.
                string usedBy = GetGearUsingStat(choice);
                string usedByText = usedBy.Length > 0 ? $"   <color=#33BFB3>· {usedBy}</color>" : string.Empty;
                TextMeshProUGUI label = UIKit.CreateText(row, $"{choice}  {value}   (Power {power:0.0}){usedByText}", 24f, TextAlignmentOptions.MidlineLeft, UIKit.TextColor);
                UIKit.FitText(label, 16f);
                string usedByTip = usedBy.Length > 0 ? $"\n\nUsed by your {usedBy}." : string.Empty;
                UITooltipTarget.Attach(label, $"{choice} {value}. Power = stat / 10 = {power:0.0}. Abilities and attacks that scale with {choice} multiply by this.{usedByTip}");
                label.raycastTarget = true; // so the tooltip can see the mouse

                Button plus = UIKit.CreateButton(row, "+1", () => SpendStat(choice), 52f);
                UIKit.SetSize(plus, 52f, 72f).flexibleWidth = 0f;
                plus.interactable = _character.CanSpendStatPoint(choice, _combatState, out string reason);
                if (!plus.interactable)
                {
                    UITooltipTarget.Attach(plus, reason);
                }
            }

            if (_bootstrap.PlayerHealth != null)
            {
                UIKit.CreateText(_statsList, $"Max HP {_bootstrap.PlayerHealth.MaxHealth}", 24f, TextAlignmentOptions.Left, UIKit.MutedText);
            }

            UIKit.CreateText(_statsList, $"Burden {_character.Loadout.BurdenSteps}/{Loadout.MaxBurdenSteps}   Move speed {Mathf.RoundToInt(_character.Loadout.MoveSpeedMultiplier * 100f)}%", 22f, TextAlignmentOptions.Left, UIKit.MutedText);
        }

        private void DrawTrees()
        {
            UIKit.ClearChildren(_treeRow);
            if (_trees.Count == 0)
            {
                TextMeshProUGUI none = UIKit.CreateText(_treeRow, "No skill trees for this archetype yet.", 26f, TextAlignmentOptions.Center, UIKit.MutedText);
                UIKit.Stretch(none.rectTransform);
                return;
            }

            CharacterSkills skills = _character.Skills;
            List<RectTransform> panels = new List<RectTransform>();
            foreach (SkillTreeDefinition tree in _trees)
            {
                RectTransform panel = UIKit.CreatePanel(_treeRow, tree.DisplayName, UIKit.ButtonColor * 0.8f);
                panels.Add(panel);
                UIKit.AddVerticalLayout(panel, 8f, 16);

                TextMeshProUGUI title = UIKit.CreateText(panel, tree.DisplayName, 30f, TextAlignmentOptions.Center, UIKit.TextColor);
                title.raycastTarget = true;
                UITooltipTarget.Attach(title, tree.Description);

                string gearNote = tree.GetEquipmentNote();
                if (gearNote != null)
                {
                    bool usable = tree.IsUsableWith(_character.Loadout);
                    UIKit.CreateText(panel, usable ? gearNote + " (worn)" : gearNote, 20f, TextAlignmentOptions.Center, usable ? UIKit.MutedText : UIKit.WarningText);
                }

                for (int i = 0; i < tree.NodeCount; i++)
                {
                    DrawNode(panel, tree, i, skills);
                }
            }

            // The trees share their area equally, pinned with anchors so they never move.
            UIKit.PlaceColumns(16f, panels.ToArray());
        }

        private void DrawNode(Transform panel, SkillTreeDefinition tree, int index, CharacterSkills skills)
        {
            SkillNode node = tree.Nodes[index];
            SkillNodeState state = skills.GetNodeState(tree, index);
            AbilityDefinition ability = node.GrantedAbilities.Count > 0 ? node.GrantedAbilities[0] : null;
            string abilityName = ability != null ? ability.DisplayName : "?";
            string tooltip = DescribeAbility(ability, node.GetUpgradeNote());

            if (state == SkillNodeState.Next)
            {
                SkillTreeDefinition choice = tree;
                Button unlock = UIKit.CreateButton(panel, $"{index + 1}. Unlock {abilityName}", () => UnlockNext(choice), 56f);
                unlock.interactable = skills.CanUnlockNextNode(tree, _combatState, out string reason);
                UITooltipTarget.Attach(unlock, unlock.interactable ? tooltip : reason + "\n\n" + tooltip);
                return;
            }

            string stateText = state == SkillNodeState.Owned ? "Owned" : "Locked";
            Color color = state == SkillNodeState.Owned ? UIKit.Accent : UIKit.MutedText;
            TextMeshProUGUI label = UIKit.CreateText(panel, $"{index + 1}. {abilityName}  ({stateText})", 24f, TextAlignmentOptions.Left, color);
            label.raycastTarget = true;
            UITooltipTarget.Attach(label, tooltip);
        }

        private void DrawOwned()
        {
            UIKit.ClearChildren(_ownedList);
            CharacterProgress progress = _character.Progress;
            UIKit.CreateText(_ownedList, $"Skill points: {progress.UnspentSkillPoints}", 26f, TextAlignmentOptions.Left, UIKit.Accent);

            UIKit.CreateText(_ownedList, "Owned abilities", 28f, TextAlignmentOptions.Left, UIKit.TextColor);
            List<OwnedSkillAbility> owned = _character.Skills.GetOwnedAbilities(_character.Loadout);
            if (owned.Count == 0)
            {
                UIKit.CreateText(_ownedList, "None yet.", 22f, TextAlignmentOptions.Left, UIKit.MutedText);
            }

            foreach (OwnedSkillAbility row in owned)
            {
                string text = row.IsActive ? row.Ability.DisplayName : $"{row.Ability.DisplayName}  (inactive: {row.InactiveReason})";
                TextMeshProUGUI label = UIKit.CreateText(_ownedList, text, 22f, TextAlignmentOptions.Left, row.IsActive ? UIKit.TextColor : UIKit.WarningText);
                label.raycastTarget = true;
                UITooltipTarget.Attach(label, DescribeAbility(row.Ability, null));
            }

            UIKit.CreateText(_ownedList, "Equipped", 28f, TextAlignmentOptions.Left, UIKit.TextColor);
            foreach (EquipmentSlot slot in WornSlots)
            {
                EquipmentDefinition item = _character.Loadout.GetEquipped(slot);
                if (item != null)
                {
                    string statNote = TryGetScalingStat(item, out StatType itemStat) ? $"  (uses {itemStat})" : string.Empty;
                    TextMeshProUGUI label = UIKit.CreateText(_ownedList, $"{slot}: {item.DisplayName}{statNote}", 22f, TextAlignmentOptions.Left, UIKit.TextColor);
                    label.raycastTarget = true;
                    UITooltipTarget.Attach(label, item.Description);
                }
            }

            foreach (EquippedAffix affix in _character.Loadout.Affixes)
            {
                UIKit.CreateText(_ownedList, $"Affix: {affix.Affix.DisplayName} ({affix.Form})", 22f, TextAlignmentOptions.Left, UIKit.TextColor);
            }
        }

        // The stat an item scales with: the weapon's or Foci's attack stat, or the armor's or Shroud's defensive stat.
        // Affixes and other items don't scale, so they return false.
        private static bool TryGetScalingStat(EquipmentDefinition item, out StatType stat)
        {
            if (item is WeaponDefinition weapon)
            {
                stat = weapon.ScalingStat;
                return true;
            }
            if (item is FociDefinition foci)
            {
                stat = foci.ScalingStat;
                return true;
            }
            if (item is ArmorDefinition armor)
            {
                stat = armor.ScalingStat;
                return true;
            }
            if (item is ShroudDefinition shroud)
            {
                stat = shroud.ScalingStat;
                return true;
            }

            stat = default;
            return false;
        }

        // Names of the worn items that scale with this stat, e.g. "Sword" or "Leather Coat". Empty if none.
        private string GetGearUsingStat(StatType stat)
        {
            string names = string.Empty;
            foreach (EquipmentSlot slot in WornSlots)
            {
                EquipmentDefinition item = _character.Loadout.GetEquipped(slot);
                if (item != null && TryGetScalingStat(item, out StatType itemStat) && itemStat == stat)
                {
                    names = names.Length == 0 ? item.DisplayName : names + ", " + item.DisplayName;
                }
            }
            return names;
        }

        // ----- Actions -----

        private void SpendStat(StatType stat)
        {
            if (!_character.TrySpendStatPoint(stat, _combatState, out string reason))
            {
                _statusText.text = reason;
            }
        }

        private void UnlockNext(SkillTreeDefinition tree)
        {
            if (!_character.Skills.TryUnlockNextNode(tree, _combatState, out string reason))
            {
                _statusText.text = reason;
            }
        }

        // ----- Helpers -----

        private static string DescribeAbility(AbilityDefinition ability, string upgradeNote)
        {
            if (ability == null)
            {
                return string.Empty;
            }

            string kind = ability.Kind == AbilityKind.Passive ? "Passive"
                : ability.Kind == AbilityKind.Defensive ? "Q (defensive)"
                : "Keys 1-5";
            string cost = ability.Kind == AbilityKind.Passive ? string.Empty : $"   Energy {ability.EnergyCost}   Cooldown {ability.Cooldown:0.#} s";
            string upgrade = string.IsNullOrEmpty(upgradeNote) ? string.Empty : upgradeNote + "\n";
            return $"{ability.DisplayName}\n{upgrade}{kind}{cost}\n\n{ability.Description}";
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "Skill Screen Canvas", 50);
            _root = canvas.gameObject;

            RectTransform backdrop = UIKit.CreatePanel(canvas.transform, "Backdrop", new Color(0f, 0f, 0f, 0.85f));
            UIKit.Stretch(backdrop);
            UIKit.AddVerticalLayout(backdrop, 16f, 48);

            UIKit.CreateText(backdrop, "Character   (Tab to close)", 48f, TextAlignmentOptions.Center, UIKit.Accent);

            // No layout group here: the three columns are pinned with anchors below (UIKit.PlaceColumns).
            RectTransform columns = UIKit.CreateGroup(backdrop, "Columns");
            UIKit.SetSize(columns, 720f);

            RectTransform statsPanel = UIKit.CreatePanel(columns, "Stats", UIKit.PanelColor);
            UIKit.AddVerticalLayout(statsPanel, 10f, 20);
            _statsList = UIKit.CreateGroup(statsPanel, "Stats List");
            UIKit.AddVerticalLayout(_statsList, 8f, 0);

            _treeRow = UIKit.CreateGroup(columns, "Trees");

            RectTransform ownedPanel = UIKit.CreatePanel(columns, "Owned", UIKit.PanelColor);
            UIKit.AddVerticalLayout(ownedPanel, 8f, 20);
            _ownedList = UIKit.CreateGroup(ownedPanel, "Owned List");
            UIKit.AddVerticalLayout(_ownedList, 6f, 0);

            // Fixed slices of the width (stats 1, trees 2, owned 1.2), so the columns never move as text changes.
            UIKit.PlaceColumns(24f, new[] { statsPanel, _treeRow, ownedPanel }, new[] { 1f, 2f, 1.2f });

            RectTransform details = UIKit.CreatePanel(backdrop, "Details", UIKit.PanelColor);
            UIKit.SetSize(details, 150f);
            UIKit.AddVerticalLayout(details, 4f, 16);
            _detailsText = UIKit.CreateText(details, string.Empty, 22f, TextAlignmentOptions.TopLeft, UIKit.TextColor);
            UIKit.FitText(_detailsText, 14f); // long ability descriptions shrink to fit the fixed-height box
            _statusText = UIKit.CreateText(backdrop, string.Empty, 24f, TextAlignmentOptions.Center, UIKit.WarningText);
        }
    }
}
