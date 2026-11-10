using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// One hit of damage, passed to IDamageable.TakeDamage. The attacker rolls the hit first (CombatRules)
    /// and sends the final amount; the target's Health applies armor/Shroud absorption.
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>Damage before absorption. Already doubled if the hit was critical.</summary>
        public int Amount { get; }

        /// <summary>Physical, Poison or Magic. Decides what can absorb it.</summary>
        public DamageType Type { get; }

        /// <summary>The GameObject that dealt the hit (unit, projectile owner, trap). Can be null.</summary>
        public GameObject Source { get; }

        /// <summary>The attacker's faction. Health ignores hits from its own faction.</summary>
        public Faction SourceFaction { get; }

        /// <summary>True if the hit was a crit, so hit feedback can show it.</summary>
        public bool IsCritical { get; }

        /// <summary>Creates a hit. Roll it with CombatRules first and pass the final amount.</summary>
        public DamageInfo(int amount, DamageType type, GameObject source, Faction sourceFaction, bool isCritical = false)
        {
            Amount = amount;
            Type = type;
            Source = source;
            SourceFaction = sourceFaction;
            IsCritical = isCritical;
        }

        /// <summary>Returns a copy of this hit with a different amount (used while armor/Shrouds absorb part of it).</summary>
        public DamageInfo WithAmount(int newAmount)
        {
            return new DamageInfo(newAmount, Type, Source, SourceFaction, IsCritical);
        }
    }
}
