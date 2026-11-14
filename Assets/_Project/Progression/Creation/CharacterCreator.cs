using System;
using System.Collections.Generic;
using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// The character creator's logic, separate from its screen: archetype -> preset -> loadout -> name -> confirm.
    /// Picking a preset builds a level-1 draft character right away; the loadout screen edits Draft.Loadout
    /// directly (the same Loadout API the station uses). Confirm turns the choices into a CharacterBlueprint.
    /// </summary>
    public class CharacterCreator
    {
        private readonly ProgressionCatalog _catalog;

        public CharacterCreator(ProgressionCatalog catalog)
        {
            _catalog = catalog;
        }

        /// <summary>
        /// Raised when the archetype, preset or name changes, so the screen can redraw. Loadout edits raise
        /// Draft.Loadout.LoadoutChanged instead.
        /// </summary>
        public event Action Changed;

        /// <summary>True once an archetype is picked.</summary>
        public bool HasArchetype { get; private set; }

        /// <summary>The picked archetype. Only meaningful when HasArchetype is true.</summary>
        public Archetype Archetype { get; private set; }

        /// <summary>The picked preset, or null.</summary>
        public PresetDefinition Preset { get; private set; }

        /// <summary>The level-1 character being edited, or null until a preset is picked.</summary>
        public CharacterData Draft { get; private set; }

        /// <summary>The typed name. Blank becomes "Unnamed Debtor" when the character is built.</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>Archetypes to show as cards.</summary>
        public List<Archetype> GetArchetypeChoices()
        {
            return _catalog.GetArchetypes();
        }

        /// <summary>Presets for the picked archetype (empty before one is picked).</summary>
        public List<PresetDefinition> GetPresetChoices()
        {
            return HasArchetype ? _catalog.GetPresets(Archetype) : new List<PresetDefinition>();
        }

        /// <summary>Items for one slot dropdown, for the picked archetype (empty before one is picked).</summary>
        public List<EquipmentDefinition> GetEquipmentChoices(EquipmentSlot slot)
        {
            return HasArchetype ? _catalog.GetEquipment(Archetype, slot) : new List<EquipmentDefinition>();
        }

        /// <summary>Picks an archetype. Changing it clears the preset and the draft.</summary>
        public void SelectArchetype(Archetype archetype)
        {
            if (HasArchetype && Archetype == archetype)
            {
                return;
            }

            HasArchetype = true;
            Archetype = archetype;
            Preset = null;
            Draft = null;
            Changed?.Invoke();
        }

        /// <summary>Picks a preset of the current archetype and builds a fresh draft from it (replacing any old draft).</summary>
        public bool TrySelectPreset(PresetDefinition preset, out string refusalReason)
        {
            if (!HasArchetype)
            {
                refusalReason = "Pick an archetype first.";
                return false;
            }

            if (preset == null || preset.Archetype != Archetype)
            {
                refusalReason = $"That preset isn't a {Archetype} preset.";
                return false;
            }

            Preset = preset;
            Draft = CharacterData.CreateFromPreset(preset, Name, _catalog.LevelCurve);
            refusalReason = null;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Sets the typed name.</summary>
        public void SetName(string characterName)
        {
            Name = characterName ?? string.Empty;
            Changed?.Invoke();
        }

        /// <summary>True once there's a draft to confirm. The UI uses it to enable the Confirm button.</summary>
        public bool CanConfirm(out string refusalReason)
        {
            if (Draft == null)
            {
                refusalReason = "Pick a preset first.";
                return false;
            }

            refusalReason = null;
            return true;
        }

        /// <summary>Turns the current choices into a blueprint for the run.</summary>
        public bool TryConfirm(out CharacterBlueprint blueprint, out string refusalReason)
        {
            if (!CanConfirm(out refusalReason))
            {
                blueprint = null;
                return false;
            }

            blueprint = CharacterBlueprint.FromDraft(Preset, Name, Draft, _catalog.LevelCurve);
            return true;
        }
    }
}
