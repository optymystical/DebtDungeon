using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Base data for every piece of equipment. Each subclass fixes its own slot and archetype, so a weapon
    /// can't be set up as Magic gear by mistake. Design data only: never change these values at runtime.
    /// </summary>
    public abstract class EquipmentDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique id, e.g. \"sword\". Used for lookups and logs; never shown to the player.")]
        [SerializeField] private string _id;

        [SerializeField] private string _displayName;

        [SerializeField, TextArea] private string _description;

        [SerializeField] private Sprite _icon;

        [Header("Effects While Worn")]
        [Tooltip("Stat bonuses while worn. They must use this item's archetype's stats.")]
        [SerializeField] private List<StatBonus> _statBonuses = new List<StatBonus>();

        [Tooltip("Abilities this item grants while worn: its basic attack, Parry, etc. (built by Player).")]
        [SerializeField] private List<AbilityDefinition> _grantedAbilities = new List<AbilityDefinition>();

        [Tooltip("What skill trees can require of this item, e.g. Bladed.")]
        [SerializeField] private List<EquipmentTag> _tags = new List<EquipmentTag>();

        /// <summary>Unique id, e.g. "sword".</summary>
        public string Id => _id;

        /// <summary>Name shown in the UI, e.g. "Sword".</summary>
        public string DisplayName => _displayName;

        /// <summary>Tooltip text.</summary>
        public string Description => _description;

        /// <summary>Icon for the loadout screen.</summary>
        public Sprite Icon => _icon;

        /// <summary>Stat bonuses while worn.</summary>
        public IReadOnlyList<StatBonus> StatBonuses => _statBonuses;

        /// <summary>Abilities granted while worn.</summary>
        public IReadOnlyList<AbilityDefinition> GrantedAbilities => _grantedAbilities;

        /// <summary>Tags skill trees can require.</summary>
        public IReadOnlyList<EquipmentTag> Tags => _tags;

        /// <summary>The slot this item goes in. Fixed by the subclass.</summary>
        public abstract EquipmentSlot Slot { get; }

        /// <summary>The only archetype that can wear this item. Fixed by the subclass.</summary>
        public abstract Archetype Archetype { get; }

        /// <summary>Burden steps this item costs while worn (each step is -10% movement). Most items cost 0.</summary>
        public virtual int BurdenSteps => 0;

        /// <summary>True if this item has the given tag (e.g. Bladed).</summary>
        public bool HasTag(EquipmentTag tag)
        {
            return _tags.Contains(tag);
        }

        /// <summary>Editor check: warns about stat bonuses for another archetype's stats, which would silently do nothing.</summary>
        protected virtual void OnValidate()
        {
            for (int i = 0; i < _statBonuses.Count; i++)
            {
                if (!StatRules.BelongsTo(_statBonuses[i].Stat, Archetype))
                {
                    Debug.LogWarning($"{name}: {_statBonuses[i].Stat} is not a {Archetype} stat, so this bonus does nothing.", this);
                }
            }
        }
    }
}
