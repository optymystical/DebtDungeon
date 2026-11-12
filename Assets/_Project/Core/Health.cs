using System;
using System.Collections.Generic;
using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// Hit points for anything that can die: the player, enemies, spawners, destructibles.
    /// Applies absorbers (armor/Shroud), ignores friendly fire, and on death raises its own Died event
    /// plus GameEvents.ActorKilled (and PlayerDied for the player). It never destroys its GameObject;
    /// the owning module decides what death looks like.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        private const string UnknownKillerName = "Unknown";

        [Header("Identity")]
        [Tooltip("Who this belongs to. Hits from the same faction are ignored.")]
        [SerializeField] private Faction _faction = Faction.Neutral;

        [Header("Health")]
        [Tooltip("Hit points at full health. Progression overrides this for the player with SetMaxHealth.")]
        [SerializeField, Min(1)] private int _maxHealth = 100;

        [Tooltip("Seconds after a hit during which further hits are ignored (i-frames). 0 = none.")]
        [SerializeField, Min(0f)] private float _invulnerabilityDuration = 0f;

        private readonly List<IDamageAbsorber> _absorbers = new List<IDamageAbsorber>();
        private IKillReward _killReward;
        private int _currentHealth;
        private float _invulnerableUntil;
        private bool _isDead;

        /// <summary>Who this belongs to.</summary>
        public Faction Faction => _faction;

        /// <summary>False once health has reached zero.</summary>
        public bool IsAlive => !_isDead;

        /// <summary>Current hit points.</summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>Hit points at full health.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>Raised when current or max health changes. Parameters: current, max. For health bars.</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>
        /// Raised for every hit that gets through the faction and i-frame checks, even if armor absorbed all of it.
        /// Parameters: the hit, and the damage actually taken after absorption.
        /// </summary>
        public event Action<DamageInfo, int> Damaged;

        /// <summary>Raised once, when health reaches zero. Use it for death animations and cleanup.</summary>
        public event Action<KillInfo> Died;

        private void Awake()
        {
            _currentHealth = _maxHealth;

            // Optional: enemies and spawners have a reward component; the player doesn't.
            TryGetComponent(out _killReward);
        }

        /// <summary>
        /// Applies one hit: ignores it if dead, invulnerable or from the same faction, then runs it through
        /// the absorbers and subtracts what's left. Raises Damaged, HealthChanged and, at zero, Died.
        /// </summary>
        public void TakeDamage(DamageInfo damage)
        {
            if (_isDead || damage.Amount <= 0)
            {
                return;
            }

            // No friendly fire. Traps use Faction.Neutral, so they still hurt every faction.
            if (damage.SourceFaction == _faction)
            {
                return;
            }

            if (Time.time < _invulnerableUntil)
            {
                return;
            }

            int damageTaken = ApplyAbsorbers(damage);
            _currentHealth = Mathf.Max(_currentHealth - damageTaken, 0);
            _invulnerableUntil = Time.time + _invulnerabilityDuration;

            Damaged?.Invoke(damage, damageTaken);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth == 0)
            {
                Die(damage);
            }
        }

        /// <summary>Restores hit points, up to max. Does nothing once dead.</summary>
        public void Heal(int amount)
        {
            if (_isDead || amount <= 0)
            {
                return;
            }

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Changes max health (Progression calls this when the defensive stat changes).
        /// With refill, current health is set to the new max; otherwise it's only clamped down to it.
        /// </summary>
        public void SetMaxHealth(int newMaxHealth, bool refill)
        {
            _maxHealth = Mathf.Max(1, newMaxHealth);
            _currentHealth = refill ? _maxHealth : Mathf.Min(_currentHealth, _maxHealth);
            HealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>Adds armor, a Shroud or another absorber. Absorbers run in the order they were added.</summary>
        public void AddAbsorber(IDamageAbsorber absorber)
        {
            if (absorber == null || _absorbers.Contains(absorber))
            {
                return;
            }

            _absorbers.Add(absorber);
        }

        /// <summary>Removes an absorber, e.g. when the loadout changes at the station.</summary>
        public void RemoveAbsorber(IDamageAbsorber absorber)
        {
            _absorbers.Remove(absorber);
        }

        /// <summary>Passes the hit through each absorber in turn and returns the damage left at the end.</summary>
        private int ApplyAbsorbers(DamageInfo damage)
        {
            int remaining = damage.Amount;

            for (int i = 0; i < _absorbers.Count && remaining > 0; i++)
            {
                int afterAbsorber = _absorbers[i].AbsorbDamage(damage.WithAmount(remaining));

                // Guard against a buggy absorber returning a negative value or adding damage.
                remaining = Mathf.Clamp(afterAbsorber, 0, remaining);
            }

            return remaining;
        }

        private void Die(DamageInfo killingBlow)
        {
            _isDead = true;
            KillInfo kill = BuildKillInfo(killingBlow);

            Died?.Invoke(kill);
            GameEvents.RaiseActorKilled(kill);

            if (_faction == Faction.Player)
            {
                GameEvents.RaisePlayerDied(kill);
            }
        }

        private KillInfo BuildKillInfo(DamageInfo killingBlow)
        {
            int xpReward = 0;
            int goldReward = 0;

            if (_killReward != null)
            {
                xpReward = _killReward.XpReward;
                goldReward = _killReward.GoldReward;
            }

            return new KillInfo(
                gameObject,
                _faction,
                killingBlow.Source,
                killingBlow.SourceFaction,
                GetDisplayName(killingBlow.Source),
                transform.position,
                xpReward,
                goldReward);
        }

        /// <summary>Uses the killer's IKillReward name if it has one ("Goblin Brute"), otherwise its GameObject name.</summary>
        private static string GetDisplayName(GameObject killer)
        {
            if (killer == null)
            {
                return UnknownKillerName;
            }

            if (killer.TryGetComponent(out IKillReward killerInfo))
            {
                return killerInfo.DisplayName;
            }

            return killer.name;
        }
    }
}
