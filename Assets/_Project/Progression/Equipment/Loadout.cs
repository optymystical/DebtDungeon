using System;
using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// What a character is wearing, and the rules for changing it: own-archetype gear only, one item per slot,
    /// no duplicate Affixes, and a burden cap of 2 steps. Applies each item's stat bonuses to the character's stats
    /// while it's worn. Plain C# (no scene needed); the creator and the station both edit it.
    /// </summary>
    public class Loadout
    {
        /// <summary>Each burden step costs 10% movement (decided Nov 12).</summary>
        public const float MoveSpeedPenaltyPerBurdenStep = 0.1f;

        /// <summary>Changes that would go over this many burden steps are refused.</summary>
        public const int MaxBurdenSteps = 2;

        /// <summary>Burden cost of an Affix that doesn't fit in a free slot.</summary>
        public const int PaidAffixBurden = 1;

        private readonly CharacterStats _stats;
        private readonly Dictionary<EquipmentSlot, EquipmentDefinition> _equipped =
            new Dictionary<EquipmentSlot, EquipmentDefinition>();
        private readonly List<EquippedAffix> _affixes = new List<EquippedAffix>();

        /// <summary>Creates an empty loadout that applies its bonuses to these stats.</summary>
        public Loadout(CharacterStats stats)
        {
            _stats = stats;
        }

        /// <summary>Raised after any successful change, so UI, abilities and absorbers can refresh.</summary>
        public event Action LoadoutChanged;

        /// <summary>The wearer's archetype. Only gear of this archetype can be worn.</summary>
        public Archetype Archetype => _stats.Archetype;

        /// <summary>Equipped Affixes and their forms.</summary>
        public IReadOnlyList<EquippedAffix> Affixes => _affixes;

        /// <summary>Total burden steps: item burden (armor weight) plus 1 per paid Affix.</summary>
        public int BurdenSteps => CalculateBurdenSteps();

        /// <summary>Movement multiplier from burden: 1.0, 0.9 or 0.8.</summary>
        public float MoveSpeedMultiplier => 1f - MoveSpeedPenaltyPerBurdenStep * BurdenSteps;

        /// <summary>The item in a single-item slot (Weapon, Armor, Foci, Shroud), or null if empty.</summary>
        public EquipmentDefinition GetEquipped(EquipmentSlot slot)
        {
            _equipped.TryGetValue(slot, out EquipmentDefinition item);
            return item;
        }

        /// <summary>True if anything worn has this tag (used by skill trees, e.g. Bladed).</summary>
        public bool HasTag(EquipmentTag tag)
        {
            foreach (EquipmentDefinition item in _equipped.Values)
            {
                if (item.HasTag(tag))
                {
                    return true;
                }
            }

            for (int i = 0; i < _affixes.Count; i++)
            {
                if (_affixes[i].Affix.HasTag(tag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every ability granted by worn gear (basic attack, Parry, ...), in slot order.</summary>
        public List<AbilityDefinition> GetGrantedAbilities()
        {
            List<AbilityDefinition> abilities = new List<AbilityDefinition>();

            foreach (EquipmentDefinition item in _equipped.Values)
            {
                abilities.AddRange(item.GrantedAbilities);
            }

            for (int i = 0; i < _affixes.Count; i++)
            {
                abilities.AddRange(_affixes[i].Affix.GrantedAbilities);
            }

            return abilities;
        }

        /// <summary>
        /// Puts an item in its slot, replacing what was there. Refused (with a reason for the UI) if the item is
        /// another archetype's, is an Affix (use TryAddAffix), or would push burden over the cap.
        /// </summary>
        public bool TryEquip(EquipmentDefinition item, out string refusalReason)
        {
            if (!CanWear(item, out refusalReason))
            {
                return false;
            }

            if (item.Slot == EquipmentSlot.Affix)
            {
                refusalReason = "Affixes are attuned with TryAddAffix.";
                return false;
            }

            EquipmentDefinition previous = GetEquipped(item.Slot);
            if (previous == item)
            {
                refusalReason = null;
                return true;
            }

            // Try the change, and undo it if burden goes over the cap.
            _equipped[item.Slot] = item;
            if (BurdenSteps > MaxBurdenSteps)
            {
                RestoreSlot(item.Slot, previous);
                refusalReason = $"{item.DisplayName} would put burden over {MaxBurdenSteps} steps.";
                return false;
            }

            if (previous != null)
            {
                _stats.RemoveModifiersFromSource(previous);
            }

            ApplyStatBonuses(item);
            refusalReason = null;
            LoadoutChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Empties a slot. Refused if it would push burden over the cap
        /// (e.g. removing a Foci whose free slots hold Affixes, which would then become paid).
        /// </summary>
        public bool TryUnequip(EquipmentSlot slot, out string refusalReason)
        {
            EquipmentDefinition previous = GetEquipped(slot);
            if (previous == null)
            {
                refusalReason = null;
                return true;
            }

            _equipped.Remove(slot);
            if (BurdenSteps > MaxBurdenSteps)
            {
                _equipped[slot] = previous;
                refusalReason = $"Removing {previous.DisplayName} would put burden over {MaxBurdenSteps} steps.";
                return false;
            }

            _stats.RemoveModifiersFromSource(previous);
            refusalReason = null;
            LoadoutChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Attunes an Affix in a form. It takes a free slot of that form if one is open (Foci for Offensive,
        /// Shroud for Defensive); otherwise it costs 1 burden step. Refused for duplicates, other archetypes' gear,
        /// or going over the burden cap.
        /// </summary>
        public bool TryAddAffix(AffixDefinition affix, AffixForm form, out string refusalReason)
        {
            if (!CanWear(affix, out refusalReason))
            {
                return false;
            }

            if (IndexOfAffix(affix) >= 0)
            {
                refusalReason = $"{affix.DisplayName} is already attuned.";
                return false;
            }

            _affixes.Add(new EquippedAffix(affix, form));
            if (BurdenSteps > MaxBurdenSteps)
            {
                _affixes.RemoveAt(_affixes.Count - 1);
                refusalReason = $"{affix.DisplayName} would put burden over {MaxBurdenSteps} steps.";
                return false;
            }

            ApplyStatBonuses(affix);
            refusalReason = null;
            LoadoutChanged?.Invoke();
            return true;
        }

        /// <summary>Removes an Affix. Removing never adds burden, so it's never refused once the Affix is found.</summary>
        public bool TryRemoveAffix(AffixDefinition affix, out string refusalReason)
        {
            int index = IndexOfAffix(affix);
            if (index < 0)
            {
                refusalReason = "That Affix isn't attuned.";
                return false;
            }

            _affixes.RemoveAt(index);
            _stats.RemoveModifiersFromSource(affix);
            refusalReason = null;
            LoadoutChanged?.Invoke();
            return true;
        }

        private bool CanWear(EquipmentDefinition item, out string refusalReason)
        {
            if (item == null)
            {
                refusalReason = "No item given.";
                return false;
            }

            if (item.Archetype != Archetype)
            {
                refusalReason = $"{item.DisplayName} is {item.Archetype} gear; this character is {Archetype}.";
                return false;
            }

            refusalReason = null;
            return true;
        }

        private int CalculateBurdenSteps()
        {
            int itemBurden = 0;
            foreach (EquipmentDefinition item in _equipped.Values)
            {
                itemBurden += item.BurdenSteps;
            }

            int offensiveCount = 0;
            int defensiveCount = 0;
            for (int i = 0; i < _affixes.Count; i++)
            {
                if (_affixes[i].Form == AffixForm.Offensive)
                {
                    offensiveCount++;
                }
                else
                {
                    defensiveCount++;
                }
            }

            FociDefinition foci = GetEquipped(EquipmentSlot.Foci) as FociDefinition;
            ShroudDefinition shroud = GetEquipped(EquipmentSlot.Shroud) as ShroudDefinition;
            int freeOffensive = foci != null ? foci.FreeOffensiveSlots : 0;
            int freeDefensive = shroud != null ? shroud.FreeDefensiveSlots : 0;

            // Affixes beyond the free slots of their form are paid.
            int paidAffixes = Math.Max(0, offensiveCount - freeOffensive) + Math.Max(0, defensiveCount - freeDefensive);
            return itemBurden + paidAffixes * PaidAffixBurden;
        }

        private void ApplyStatBonuses(EquipmentDefinition item)
        {
            for (int i = 0; i < item.StatBonuses.Count; i++)
            {
                StatBonus bonus = item.StatBonuses[i];
                _stats.AddModifier(new StatModifier(bonus.Stat, bonus.Value, item));
            }
        }

        private void RestoreSlot(EquipmentSlot slot, EquipmentDefinition previous)
        {
            if (previous == null)
            {
                _equipped.Remove(slot);
            }
            else
            {
                _equipped[slot] = previous;
            }
        }

        private int IndexOfAffix(AffixDefinition affix)
        {
            for (int i = 0; i < _affixes.Count; i++)
            {
                if (_affixes[i].Affix == affix)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
