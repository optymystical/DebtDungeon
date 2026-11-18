using System;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Runtime state for armor or a Shroud: a Durability pool that soaks up to Limit damage per hit.
    /// Registered with Core's Health as an IDamageAbsorber. The definition asset stays untouched; the pool lives here.
    /// Durability and Limit scale with the wearer's defensive stat (Flow State: "per 10 points"), see ApplyStatPower.
    /// </summary>
    public abstract class EquipmentAbsorber : IDamageAbsorber
    {
        // The item's listed values: what it has at Power 1 (10 in its stat). Scaled values are worked out from these,
        // never from the last scaled value, so repeated stat changes can't drift.
        private readonly int _baseMaxDurability;
        private readonly int _baseLimit;

        private int _limit;
        private int _maxDurability;
        private float _currentDurability;

        /// <summary>Starts at full Durability, unscaled (multiplier 1).</summary>
        protected EquipmentAbsorber(int maxDurability, int limit)
        {
            _baseMaxDurability = maxDurability;
            _baseLimit = limit;
            _maxDurability = maxDurability;
            _limit = limit;
            _currentDurability = maxDurability;
            StatMultiplier = 1f;
        }

        /// <summary>The stat that scales this item: Constitution for armor, Build for Shrouds.</summary>
        public abstract StatType ScalingStat { get; }

        /// <summary>The multiplier currently applied to the listed values (1 or more).</summary>
        public float StatMultiplier { get; private set; }

        /// <summary>Most damage absorbed from a single hit, after scaling.</summary>
        public int Limit => _limit;

        /// <summary>Raised when Durability changes (a hit, regeneration or a repair). Parameters: current, max.</summary>
        public event Action<int, int> DurabilityChanged;

        /// <summary>Current Durability, rounded down.</summary>
        public int CurrentDurability => Mathf.FloorToInt(_currentDurability);

        /// <summary>Durability when full.</summary>
        public int MaxDurability => _maxDurability;

        /// <summary>True when the pool is empty and nothing is absorbed.</summary>
        public bool IsDepleted => CurrentDurability <= 0;

        /// <summary>
        /// Soaks up what it can from the hit: up to Limit, never more than the Durability left, and only for damage
        /// types it blocks. Returns the damage that gets through to Health.
        /// </summary>
        public int AbsorbDamage(DamageInfo damage)
        {
            OnHit();

            if (!Blocks(damage.Type) || IsDepleted)
            {
                return damage.Amount;
            }

            int absorbed = Mathf.Min(damage.Amount, _limit, CurrentDurability);
            _currentDurability -= absorbed;
            RaiseDurabilityChanged();
            return damage.Amount - absorbed;
        }

        /// <summary>
        /// Scales Durability and Limit by the wearer's Power in ScalingStat (stat / 10, Core StatRules), never below the
        /// item's listed values. Keeps the current Durability percentage, like Health.RescaleMaxHealth, so raising
        /// Constitution mid-fight doesn't hand out free Durability. PlayerDefenses calls this when stats change.
        /// </summary>
        public void ApplyStatPower(float power)
        {
            float multiplier = Mathf.Max(1f, power);
            int newMax = Mathf.Max(1, Mathf.RoundToInt(_baseMaxDurability * multiplier));
            int newLimit = Mathf.Max(1, Mathf.RoundToInt(_baseLimit * multiplier));
            StatMultiplier = multiplier;

            if (newMax == _maxDurability && newLimit == _limit)
            {
                return;
            }

            float fractionLeft = _currentDurability / _maxDurability;
            _maxDurability = newMax;
            _limit = newLimit;
            _currentDurability = fractionLeft * newMax;
            RaiseDurabilityChanged();
        }

        /// <summary>Which damage types this absorbs. Armor: Physical. Shroud: Physical and Magic.</summary>
        protected abstract bool Blocks(DamageType type);

        /// <summary>Called on every hit, blocked or not. Shrouds use it to restart their regen delay.</summary>
        protected virtual void OnHit()
        {
        }

        /// <summary>Restores Durability, capped at max.</summary>
        protected void RestoreDurability(float amount)
        {
            if (amount <= 0f || _currentDurability >= _maxDurability)
            {
                return;
            }

            int before = CurrentDurability;
            _currentDurability = Mathf.Min(_currentDurability + amount, _maxDurability);

            // Only announce whole-point changes, so regen doesn't fire the event every frame.
            if (CurrentDurability != before)
            {
                RaiseDurabilityChanged();
            }
        }

        private void RaiseDurabilityChanged()
        {
            DurabilityChanged?.Invoke(CurrentDurability, _maxDurability);
        }
    }
}
