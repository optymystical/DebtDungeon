using System;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A stat bonus written on a piece of equipment in the Inspector (e.g. +2 Dexterity).
    /// The loadout turns it into a StatModifier, with the equipment as its source, while the item is worn.
    /// </summary>
    [Serializable]
    public struct StatBonus
    {
        [SerializeField] private StatType _stat;
        [SerializeField] private int _value;

        /// <summary>The stat this bonus changes. Must belong to the item's archetype, or it does nothing.</summary>
        public StatType Stat => _stat;

        /// <summary>The amount added while the item is worn. Negative values are penalties.</summary>
        public int Value => _value;
    }
}
