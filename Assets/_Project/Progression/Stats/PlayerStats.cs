using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Puts a character's stats on the player and keeps max HP in sync with the archetype's defensive stat
    /// (Constitution, Build or Willpower). Game calls Initialize with the character from the creator.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public class PlayerStats : MonoBehaviour
    {
        [Header("Max Health (GDD: max HP comes from the defensive stat)")]
        [Tooltip("Max HP with 0 points in the defensive stat.")]
        [SerializeField, Min(1)] private int _baseMaxHealth = 50;

        [Tooltip("Max HP gained per point of the defensive stat (Constitution, Build or Willpower).")]
        [SerializeField, Min(0)] private int _healthPerDefensivePoint = 5;

        private Health _health;
        private CharacterStats _stats;
        private bool _isSubscribed;

        /// <summary>The player's stats, or null until Initialize is called. Other modules should read them as IStatProvider.</summary>
        public CharacterStats Stats => _stats;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Gives the player a character's stats and sets max HP from them at full health.
        /// Call once when the player spawns (Game does this with the character from the creator).
        /// </summary>
        public void Initialize(CharacterStats stats)
        {
            if (stats == null)
            {
                Debug.LogError("PlayerStats.Initialize was given no stats.", this);
                return;
            }

            Unsubscribe();
            _stats = stats;
            Subscribe();

            // A new character starts at full health.
            _health.SetMaxHealth(CalculateMaxHealth(_stats, _baseMaxHealth, _healthPerDefensivePoint), true);
        }

        /// <summary>
        /// Max HP = base + defensive stat x HP per point. Static and separate from the component so it can be tested
        /// and reused (e.g. the creator UI can preview a preset's HP).
        /// </summary>
        public static int CalculateMaxHealth(IStatProvider stats, int baseMaxHealth, int healthPerDefensivePoint)
        {
            StatType defensiveStat = StatRules.GetDefensiveStat(stats.Archetype);
            return baseMaxHealth + stats.GetStat(defensiveStat) * healthPerDefensivePoint;
        }

        private void OnStatsChanged()
        {
            // Mid-run changes keep the same HP percentage (Trello: "keeping the current HP percentage").
            _health.RescaleMaxHealth(CalculateMaxHealth(_stats, _baseMaxHealth, _healthPerDefensivePoint));
        }

        private void Subscribe()
        {
            if (_stats == null || _isSubscribed || !isActiveAndEnabled)
            {
                return;
            }

            _stats.StatsChanged += OnStatsChanged;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (_stats == null || !_isSubscribed)
            {
                return;
            }

            _stats.StatsChanged -= OnStatsChanged;
            _isSubscribed = false;
        }
    }
}
