using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// One list of all player-facing progression content: presets, equipment, skill trees and the level curve.
    /// The creator, the skill tree screen and the station read their choices from here, so adding an item or a
    /// tree means adding it to this asset, not changing code. Design data only: never change it at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressionCatalog", menuName = "Debt Dungeon/Progression Catalog")]
    public class ProgressionCatalog : ScriptableObject
    {
        [SerializeField] private LevelCurveDefinition _levelCurve;

        [Tooltip("In the order the creator shows them.")]
        [SerializeField] private List<PresetDefinition> _presets = new List<PresetDefinition>();

        [Tooltip("Every weapon, armor, Foci, Shroud and Affix the player can pick.")]
        [SerializeField] private List<EquipmentDefinition> _equipment = new List<EquipmentDefinition>();

        [SerializeField] private List<SkillTreeDefinition> _skillTrees = new List<SkillTreeDefinition>();

        /// <summary>The level curve every new character uses.</summary>
        public LevelCurveDefinition LevelCurve => _levelCurve;

        /// <summary>
        /// The archetypes that have at least one preset, in preset order. Mental has none in Alpha,
        /// so the creator only offers Martial and Magic.
        /// </summary>
        public List<Archetype> GetArchetypes()
        {
            List<Archetype> archetypes = new List<Archetype>();
            foreach (PresetDefinition preset in _presets)
            {
                if (preset != null && !archetypes.Contains(preset.Archetype))
                {
                    archetypes.Add(preset.Archetype);
                }
            }

            return archetypes;
        }

        /// <summary>This archetype's presets.</summary>
        public List<PresetDefinition> GetPresets(Archetype archetype)
        {
            List<PresetDefinition> presets = new List<PresetDefinition>();
            foreach (PresetDefinition preset in _presets)
            {
                if (preset != null && preset.Archetype == archetype)
                {
                    presets.Add(preset);
                }
            }

            return presets;
        }

        /// <summary>This archetype's items for one slot (Affix included), e.g. every Martial weapon.</summary>
        public List<EquipmentDefinition> GetEquipment(Archetype archetype, EquipmentSlot slot)
        {
            List<EquipmentDefinition> items = new List<EquipmentDefinition>();
            foreach (EquipmentDefinition item in _equipment)
            {
                if (item != null && item.Archetype == archetype && item.Slot == slot)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>This archetype's skill trees.</summary>
        public List<SkillTreeDefinition> GetSkillTrees(Archetype archetype)
        {
            List<SkillTreeDefinition> trees = new List<SkillTreeDefinition>();
            foreach (SkillTreeDefinition tree in _skillTrees)
            {
                if (tree != null && tree.Archetype == archetype)
                {
                    trees.Add(tree);
                }
            }

            return trees;
        }

        /// <summary>Editor check: warns about empty entries and a missing level curve.</summary>
        private void OnValidate()
        {
            if (_levelCurve == null)
            {
                Debug.LogWarning($"{name}: no level curve assigned.", this);
            }

            if (_presets.Contains(null) || _equipment.Contains(null) || _skillTrees.Contains(null))
            {
                Debug.LogWarning($"{name}: has an empty list entry.", this);
            }
        }
    }
}
