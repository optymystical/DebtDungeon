using System;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Runtime state for armor or a Shroud: a Durability pool that soaks up to Limit damage per hit.
    /// Registered with Core's Health as an IDamageAbsorber. The definition asset stays untouched; the pool lives here.
    /// </summary>
    public abstract class EquipmentAbsorber : IDamageAbsorber
    {
        private readonly int _limit;
        private readonly int _maxDurability;
        private float _currentDurability;

        /// <summary>Starts at full Durability.</summary>
        protected EquipmentAbsorber(int maxDurability, int limit)
        {
            _maxDurability = maxDurability;
            _limit = limit;
            _currentDurability = maxDurability;
        }

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
