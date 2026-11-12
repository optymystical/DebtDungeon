using System;
using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A character's runtime stats: a base value per stat plus any modifiers (gear, skills, effects).
    /// Only the character's own archetype's 3 stats count; everything else reads as 0 and is ignored.
    /// A plain C# object (not a component), so it can be built in the creator, carried into the run, and tested on its own.
    /// </summary>
    public class CharacterStats : IStatProvider
    {
        private static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        private readonly int[] _baseValues;
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        /// <summary>Creates a character with every base stat at 0. Set the preset's values with SetBaseStat.</summary>
        public CharacterStats(Archetype archetype)
        {
            Archetype = archetype;
            _baseValues = new int[StatCount];
        }

        /// <summary>The character's archetype. Only its 3 stats count.</summary>
        public Archetype Archetype { get; }

        /// <summary>Raised whenever a final stat value may have changed (base value or modifiers).</summary>
        public event Action StatsChanged;

        /// <summary>
        /// The stat's final value: base plus all modifiers, never below 0.
        /// Returns 0 for any stat outside the character's archetype.
        /// </summary>
        public int GetStat(StatType stat)
        {
            if (!StatRules.BelongsTo(stat, Archetype))
            {
                return 0;
            }

            int total = _baseValues[(int)stat];

            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].Stat == stat)
                {
                    total += _modifiers[i].Value;
                }
            }

            // A big enough penalty could push a stat negative; 0 is the floor (Power can't go below 0).
            return Math.Max(0, total);
        }

        /// <summary>Power for a stat: its final value / 10 (GDD: Power). Multiplies an ability's base values.</summary>
        public float GetPower(StatType stat)
        {
            return StatRules.GetPower(GetStat(stat));
        }

        /// <summary>The stat's base value, without modifiers. 0 for stats outside the archetype.</summary>
        public int GetBaseStat(StatType stat)
        {
            if (!StatRules.BelongsTo(stat, Archetype))
            {
                return 0;
            }

            return _baseValues[(int)stat];
        }

        /// <summary>
        /// Sets a base value (from a preset, or after spending stat points). Ignored for stats outside the archetype.
        /// Negative values are raised to 0. Raises StatsChanged if the value changed.
        /// </summary>
        public void SetBaseStat(StatType stat, int value)
        {
            if (!StatRules.BelongsTo(stat, Archetype))
            {
                return;
            }

            int newValue = Math.Max(0, value);
            if (_baseValues[(int)stat] == newValue)
            {
                return;
            }

            _baseValues[(int)stat] = newValue;
            StatsChanged?.Invoke();
        }

        /// <summary>
        /// Adds a modifier. Ignored if it targets a stat outside the archetype or has no source
        /// (a modifier without a source could never be removed). Raises StatsChanged.
        /// </summary>
        public void AddModifier(StatModifier modifier)
        {
            if (!StatRules.BelongsTo(modifier.Stat, Archetype) || modifier.Source == null)
            {
                return;
            }

            _modifiers.Add(modifier);
            StatsChanged?.Invoke();
        }

        /// <summary>Removes every modifier added by this source. Raises StatsChanged if anything was removed.</summary>
        public void RemoveModifiersFromSource(object source)
        {
            if (source == null)
            {
                return;
            }

            bool removedAny = false;

            // Walk backwards so removing an item doesn't skip the one after it.
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_modifiers[i].Source, source))
                {
                    _modifiers.RemoveAt(i);
                    removedAny = true;
                }
            }

            if (removedAny)
            {
                StatsChanged?.Invoke();
            }
        }
    }
}
