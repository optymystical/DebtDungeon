using System.Collections.Generic;
using UnityEngine;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Everything the creator decided: preset, name, gear and Affixes. It's a recipe, not a character:
    /// every run (including each Retry) builds a fresh level-1 CharacterData from it with Build().
    /// Read-only once made.
    /// </summary>
    public class CharacterBlueprint
    {
        private readonly List<EquipmentDefinition> _equipment;
        private readonly List<EquippedAffix> _affixes;

        public CharacterBlueprint(
            PresetDefinition preset,
            string characterName,
            List<EquipmentDefinition> equipment,
            List<EquippedAffix> affixes,
            LevelCurveDefinition levelCurve)
        {
            Preset = preset;
            Name = characterName;
            _equipment = new List<EquipmentDefinition>(equipment);
            _affixes = new List<EquippedAffix>(affixes);
            LevelCurve = levelCurve;
        }

        /// <summary>The preset the stats come from.</summary>
        public PresetDefinition Preset { get; }

        /// <summary>The name typed in the creator (may be blank; CharacterData turns blank into "Unnamed Debtor").</summary>
        public string Name { get; }

        /// <summary>Worn items, one per slot.</summary>
        public IReadOnlyList<EquipmentDefinition> Equipment => _equipment;

        /// <summary>Attuned Affixes with their forms.</summary>
        public IReadOnlyList<EquippedAffix> Affixes => _affixes;

        /// <summary>The level curve for the built character.</summary>
        public LevelCurveDefinition LevelCurve { get; }

        /// <summary>Copies a preset, a name and the current gear of a draft character into a blueprint.</summary>
        public static CharacterBlueprint FromDraft(PresetDefinition preset, string characterName, CharacterData draft, LevelCurveDefinition levelCurve)
        {
            List<EquipmentDefinition> equipment = new List<EquipmentDefinition>();
            EquipmentSlot[] wornSlots = { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Foci, EquipmentSlot.Shroud };
            foreach (EquipmentSlot slot in wornSlots)
            {
                EquipmentDefinition item = draft.Loadout.GetEquipped(slot);
                if (item != null)
                {
                    equipment.Add(item);
                }
            }

            List<EquippedAffix> affixes = new List<EquippedAffix>(draft.Loadout.Affixes);
            return new CharacterBlueprint(preset, characterName, equipment, affixes, levelCurve);
        }

        /// <summary>Builds a fresh level-1 character: the preset's stats, this blueprint's gear and name.</summary>
        public CharacterData Build()
        {
            return CharacterData.CreateFromBlueprint(this);
        }

        /// <summary>
        /// Equips this blueprint's gear onto an empty loadout: items first, then Affixes, because Affix burden
        /// depends on the free slots the Foci and Shroud give. Returns false (and logs why) if anything is refused.
        /// </summary>
        public bool ApplyLoadout(Loadout loadout)
        {
            bool allApplied = true;

            foreach (EquipmentDefinition item in _equipment)
            {
                if (!loadout.TryEquip(item, out string reason))
                {
                    Debug.LogError($"Blueprint: {item.name} refused. {reason}");
                    allApplied = false;
                }
            }

            foreach (EquippedAffix affix in _affixes)
            {
                if (!loadout.TryAddAffix(affix.Affix, affix.Form, out string reason))
                {
                    Debug.LogError($"Blueprint: {affix.Affix.name} refused. {reason}");
                    allApplied = false;
                }
            }

            return allApplied;
        }
    }
}
