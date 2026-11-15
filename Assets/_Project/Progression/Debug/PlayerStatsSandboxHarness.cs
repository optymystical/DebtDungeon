using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Sandbox-only test helper for Chris_Progression: builds a test character so PlayerStats can be tried without
    /// the creator. Right-click the component header in Play mode for the test actions. Not for the Main scene.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(Health))]
    public class PlayerStatsSandboxHarness : MonoBehaviour
    {
        private const int StatStep = 5;
        private const int TestDamage = 20;

        [Header("Test Character")]
        [SerializeField] private Archetype _archetype = Archetype.Martial;

        [Tooltip("Starting value for the archetype's defensive stat.")]
        [SerializeField, Min(0)] private int _startingDefensiveStat = 10;

        private PlayerStats _playerStats;
        private Health _health;

        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            _health = GetComponent<Health>();
        }

        private void Start()
        {
            CharacterStats stats = new CharacterStats(_archetype);
            stats.SetBaseStat(StatRules.GetDefensiveStat(_archetype), _startingDefensiveStat);
            _playerStats.Initialize(stats);
            LogHealth("Start");
        }

        [ContextMenu("Test: +5 defensive stat")]
        private void RaiseDefensiveStat()
        {
            ChangeDefensiveStat(StatStep);
        }

        [ContextMenu("Test: -5 defensive stat")]
        private void LowerDefensiveStat()
        {
            ChangeDefensiveStat(-StatStep);
        }

        [ContextMenu("Test: take 20 damage")]
        private void TakeTestDamage()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // Neutral source, like a trap, so the player's own faction filter doesn't block it.
            _health.TakeDamage(new DamageInfo(TestDamage, DamageType.Physical, gameObject, Faction.Neutral));
            LogHealth("After 20 damage");
        }

        private void ChangeDefensiveStat(int amount)
        {
            if (!Application.isPlaying || _playerStats.Stats == null)
            {
                return;
            }

            StatType defensiveStat = StatRules.GetDefensiveStat(_archetype);
            CharacterStats stats = _playerStats.Stats;
            stats.SetBaseStat(defensiveStat, stats.GetBaseStat(defensiveStat) + amount);
            LogHealth($"{defensiveStat} now {stats.GetStat(defensiveStat)}");
        }

        private void LogHealth(string label)
        {
            Debug.Log($"[Sandbox] {label}: HP {_health.CurrentHealth} / {_health.MaxHealth}", this);
        }
    }
}
