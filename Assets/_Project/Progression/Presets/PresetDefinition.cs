using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// A ready-made character: archetype, role, the three starting stats and a default loadout.
    /// The creator builds a character from one of these, then lets the player change the loadout.
    /// Design data only: never change these values at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "Preset_New", menuName = "Debt Dungeon/Preset")]
    public class PresetDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName;

        [SerializeField, TextArea] private string _description;

        [SerializeField] private Archetype _archetype = Archetype.Martial;

        [SerializeField] private PresetRole _role = PresetRole.Offense;

        [Header("Starting Stats (in the archetype's order; presets total 40)")]
        [Tooltip("Martial: Strength. Magic: Reach.")]
        [SerializeField, Min(0)] private int _firstAttackStat = 10;

        [Tooltip("Martial: Dexterity. Magic: Grasp.")]
        [SerializeField, Min(0)] private int _secondAttackStat = 10;

        [Tooltip("Martial: Constitution. Magic: Build. Drives max HP.")]
        [SerializeField, Min(0)] private int _defensiveStat = 20;

        [Header("Default Loadout")]
        [Tooltip("Weapon, armor, Foci or Shroud the character starts with. Must all be this archetype's gear.")]
        [SerializeField] private List<EquipmentDefinition> _startingEquipment = new List<EquipmentDefinition>();

        /// <summary>Name shown in the creator, e.g. "Bladesworn".</summary>
        public string DisplayName => _displayName;

        /// <summary>Creator description.</summary>
        public string Description => _description;

        /// <summary>The preset's archetype.</summary>
        public Archetype Archetype => _archetype;

        /// <summary>Offense or Defense.</summary>
        public PresetRole Role => _role;

        /// <summary>The default gear, in the order it's equipped.</summary>
        public IReadOnlyList<EquipmentDefinition> StartingEquipment => _startingEquipment;

        /// <summary>The starting value for one of the archetype's stats. 0 for other archetypes' stats.</summary>
        public int GetStartingStat(StatType stat)
        {
            if (!StatRules.BelongsTo(stat, _archetype))
            {
                return 0;
            }

            // Position within the archetype's group of 3: 0 and 1 are attack stats, 2 is the defensive stat.
            int slot = (int)stat % StatRules.StatsPerArchetype;
            switch (slot)
            {
                case 0:
                    return _firstAttackStat;
                case 1:
                    return _secondAttackStat;
                default:
                    return _defensiveStat;
            }
        }

        /// <summary>Builds a fresh character's stats from this preset.</summary>
        public CharacterStats CreateStats()
        {
            CharacterStats stats = new CharacterStats(_archetype);
            int firstStat = (int)_archetype * StatRules.StatsPerArchetype;

            for (int i = 0; i < StatRules.StatsPerArchetype; i++)
            {
                StatType stat = (StatType)(firstStat + i);
                stats.SetBaseStat(stat, GetStartingStat(stat));
            }

            return stats;
        }

        /// <summary>
        /// Equips the default gear onto a loadout. Returns false (and logs why) if any item is refused,
        /// which means the preset asset is set up wrong.
        /// </summary>
        public bool TryApplyStartingLoadout(Loadout loadout)
        {
            bool allEquipped = true;

            for (int i = 0; i < _startingEquipment.Count; i++)
            {
                if (!loadout.TryEquip(_startingEquipment[i], out string refusalReason))
                {
                    Debug.LogError($"{name}: starting item refused. {refusalReason}", this);
                    allEquipped = false;
                }
            }

            return allEquipped;
        }

        /// <summary>Editor check: warns about gear from another archetype, which the loadout would refuse.</summary>
        private void OnValidate()
        {
            for (int i = 0; i < _startingEquipment.Count; i++)
            {
                EquipmentDefinition item = _startingEquipment[i];
                if (item != null && item.Archetype != _archetype)
                {
                    Debug.LogWarning($"{name}: {item.name} is {item.Archetype} gear, but this is a {_archetype} preset.", this);
                }
            }
        }
    }
}
