using DebtDungeon.Core;
using DebtDungeon.Player;
using DebtDungeon.Progression;
using TMPro;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The in-game HUD: name and level, health, armor or Shroud, XP, gold, status badges (Slowed, Poisoned,
    /// In combat), and a reminder when points are waiting to be spent.
    /// Energy and the ability bar come with the Ability framework.
    /// Reads values every frame but only rewrites text when something changed.
    /// Put it on the GameManager's object in the Main scene, next to PlayerBootstrap and RunStatsTracker.
    /// </summary>
    [DisallowMultipleComponent]
    public class HudScreen : MonoBehaviour
    {
        private static readonly Color HealthColor = new Color(0.8f, 0.25f, 0.25f, 1f);
        private static readonly Color ArmorColor = new Color(0.75f, 0.7f, 0.6f, 1f);
        private static readonly Color ShroudColor = new Color(0.55f, 0.45f, 0.95f, 1f);
        private static readonly Color XpColor = new Color(0.95f, 0.8f, 0.3f, 1f);
        private static readonly Color SlowedColor = new Color(0.45f, 0.7f, 1f, 1f);
        private static readonly Color PoisonedColor = new Color(0.55f, 0.9f, 0.35f, 1f);
        private static readonly Color CombatColor = new Color(0.95f, 0.55f, 0.4f, 1f);

        [Tooltip("Leave empty to use the PlayerBootstrap on this object.")]
        [SerializeField] private PlayerBootstrap _bootstrap;

        [Tooltip("Leave empty to use the RunStatsTracker on this object.")]
        [SerializeField] private RunStatsTracker _stats;

        private GameObject _root;
        private TextMeshProUGUI _nameText;
        private HudBar _healthBar;
        private HudBar _defenseBar;
        private HudBar _xpBar;
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _pointsText;
        private GameObject _slowedBadge;
        private GameObject _poisonedBadge;
        private GameObject _combatBadge;

        // Found once on the spawned player (Game may reference Player). Either can be missing on a test prefab.
        private StatusEffects _statusEffects;
        private ICombatState _combatState;
        private bool _hasLookedForPlayerParts;

        private string _lastName;
        private int _lastLevel = -1;
        private int _lastGold = -1;
        private int _lastStatPoints = -1;
        private int _lastSkillPoints = -1;

        private void Awake()
        {
            if (_bootstrap == null)
            {
                _bootstrap = GetComponent<PlayerBootstrap>();
            }

            if (_stats == null)
            {
                _stats = GetComponent<RunStatsTracker>();
            }

            Build();
        }

        private void OnEnable()
        {
            GameEvents.GameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.GameStateChanged -= OnGameStateChanged;
        }

        // Hidden behind the end screens; stays visible (dimmed by the backdrop) while paused.
        private void OnGameStateChanged(GameState previous, GameState current)
        {
            _root.SetActive(current != GameState.Dead && current != GameState.Won);
        }

        private void Update()
        {
            if (_bootstrap == null || _bootstrap.Character == null)
            {
                return;
            }

            CharacterData character = _bootstrap.Character;
            CharacterProgress progress = character.Progress;

            // Only rebuild the string when the name or level changed, not every frame.
            if (character.Name != _lastName || progress.Level != _lastLevel)
            {
                _lastName = character.Name;
                _lastLevel = progress.Level;
                _nameText.text = $"{_lastName}   Lv {_lastLevel}";
            }

            if (_bootstrap.PlayerHealth != null)
            {
                _healthBar.Set(_bootstrap.PlayerHealth.CurrentHealth, _bootstrap.PlayerHealth.MaxHealth);
            }

            UpdateDefenseBar();
            UpdateStatusBadges();

            if (progress.IsMaxLevel)
            {
                _xpBar.SetLabel("XP  MAX", 1f);
            }
            else
            {
                _xpBar.Set(progress.CurrentXp, progress.XpToNextLevel, "XP");
            }

            int gold = _stats != null ? _stats.Gold : 0;
            if (gold != _lastGold)
            {
                _lastGold = gold;
                _goldText.text = $"Gold  {gold}";
            }

            if (progress.UnspentStatPoints != _lastStatPoints || progress.UnspentSkillPoints != _lastSkillPoints)
            {
                _lastStatPoints = progress.UnspentStatPoints;
                _lastSkillPoints = progress.UnspentSkillPoints;
                bool hasPoints = _lastStatPoints > 0 || _lastSkillPoints > 0;
                _pointsText.text = hasPoints ? $"Tab: {_lastStatPoints} stat / {_lastSkillPoints} skill points to spend" : string.Empty;
            }
        }

        private void UpdateStatusBadges()
        {
            // PlayerBootstrap spawns the player in its Awake, so it exists by the first Update; look only once.
            if (!_hasLookedForPlayerParts && _bootstrap.PlayerObject != null)
            {
                _hasLookedForPlayerParts = true;
                _statusEffects = _bootstrap.PlayerObject.GetComponent<StatusEffects>();
                _combatState = _bootstrap.PlayerObject.GetComponent<ICombatState>();
            }

            // SetActive only when the value changes; Unity ignores a SetActive to the same value anyway,
            // but checking activeSelf first keeps it obvious.
            SetBadge(_slowedBadge, _statusEffects != null && _statusEffects.IsSlowed);
            SetBadge(_poisonedBadge, _statusEffects != null && _statusEffects.IsPoisoned);
            SetBadge(_combatBadge, _combatState != null && _combatState.InCombat);
        }

        private static void SetBadge(GameObject badge, bool visible)
        {
            if (badge.activeSelf != visible)
            {
                badge.SetActive(visible);
            }
        }

        private void UpdateDefenseBar()
        {
            PlayerDefenses defenses = _bootstrap.Defenses;
            EquipmentAbsorber absorber = null;
            string label = string.Empty;
            Color color = ArmorColor;

            if (defenses != null && defenses.Armor != null)
            {
                absorber = defenses.Armor;
                label = "Armor";
            }
            else if (defenses != null && defenses.Shroud != null)
            {
                absorber = defenses.Shroud;
                label = "Shroud";
                color = ShroudColor;
            }

            _defenseBar.Root.SetActive(absorber != null);
            if (absorber != null)
            {
                _defenseBar.SetColor(color);
                _defenseBar.Set(absorber.CurrentDurability, absorber.MaxDurability, label);
            }
        }

        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas(transform, "HUD Canvas", 0);
            _root = canvas.gameObject;

            // Top-left block, inside the safe 90% of the screen: anchored 5% in from the left and top edges.
            RectTransform block = UIKit.CreateGroup(canvas.transform, "Status");
            block.anchorMin = new Vector2(0.05f, 0.95f);
            block.anchorMax = new Vector2(0.05f, 0.95f);
            block.pivot = new Vector2(0f, 1f);
            block.anchoredPosition = Vector2.zero;
            block.sizeDelta = new Vector2(480f, 300f);
            UIKit.AddVerticalLayout(block, 8f, 0).childAlignment = TextAnchor.UpperLeft;

            _nameText = UIKit.CreateText(block, string.Empty, 30f, TextAlignmentOptions.Left, UIKit.TextColor);
            _healthBar = new HudBar(block, "Health", HealthColor);
            _defenseBar = new HudBar(block, "Defense", ArmorColor);
            _xpBar = new HudBar(block, "XP", XpColor);
            _goldText = UIKit.CreateText(block, string.Empty, 26f, TextAlignmentOptions.Left, XpColor);
            _pointsText = UIKit.CreateText(block, string.Empty, 22f, TextAlignmentOptions.Left, UIKit.Accent);

            RectTransform badges = UIKit.CreateGroup(block, "Status Badges");
            UIKit.AddHorizontalLayout(badges, 8f, 0).childAlignment = TextAnchor.MiddleLeft;
            UIKit.SetSize(badges, 30f);
            _slowedBadge = CreateBadge(badges, "Slowed", SlowedColor);
            _poisonedBadge = CreateBadge(badges, "Poisoned", PoisonedColor);
            _combatBadge = CreateBadge(badges, "In combat", CombatColor);
        }

        // A small dark tag with colored text. Starts hidden; UpdateStatusBadges shows it while the effect is on.
        private static GameObject CreateBadge(Transform parent, string label, Color color)
        {
            RectTransform badge = UIKit.CreatePanel(parent, label + " Badge", new Color(0f, 0f, 0f, 0.6f));
            UIKit.SetSize(badge, 30f, 130f).flexibleWidth = 0f;
            TextMeshProUGUI text = UIKit.CreateText(badge, label, 20f, TextAlignmentOptions.Center, color);
            UIKit.Stretch(text.rectTransform);
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        /// <summary>
        /// One labelled bar: a dark background with a colored fill whose right edge sits at the filled fraction.
        /// Moving the fill's anchor works without a sprite (an Image's Filled mode needs one).
        /// </summary>
        private class HudBar
        {
            private readonly RectTransform _fill;
            private readonly UnityEngine.UI.Image _fillImage;
            private readonly TextMeshProUGUI _label;
            private readonly string _name;
            private int _lastCurrent = -1;
            private int _lastMax = -1;

            public HudBar(Transform parent, string name, Color color)
            {
                _name = name;
                RectTransform background = UIKit.CreatePanel(parent, name + " Bar", new Color(0f, 0f, 0f, 0.6f));
                UIKit.SetSize(background, 30f);
                Root = background.gameObject;

                _fill = UIKit.CreatePanel(background, "Fill", color);
                UIKit.Stretch(_fill);
                _fillImage = _fill.GetComponent<UnityEngine.UI.Image>();

                _label = UIKit.CreateText(background, string.Empty, 20f, TextAlignmentOptions.Center, UIKit.TextColor);
                UIKit.Stretch(_label.rectTransform);
            }

            public GameObject Root { get; }

            public void SetColor(Color color)
            {
                _fillImage.color = color;
            }

            public void Set(int current, int max, string labelPrefix = null)
            {
                if (current == _lastCurrent && max == _lastMax)
                {
                    return;
                }

                _lastCurrent = current;
                _lastMax = max;
                float fraction = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
                SetLabel($"{labelPrefix ?? _name}  {current} / {max}", fraction);
            }

            public void SetLabel(string text, float fraction)
            {
                _fill.anchorMax = new Vector2(fraction, 1f);
                _label.text = text;
            }
        }
    }
}
