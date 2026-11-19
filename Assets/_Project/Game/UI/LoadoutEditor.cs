using System;
using System.Collections.Generic;
using DebtDungeon.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The loadout rows shared by the character creator and the equipment station: one "&lt; item &gt;" row per
    /// slot, then the affix rows. A plain class (not a component): each screen makes one, points it at a list
    /// container, and calls Draw whenever the loadout changes. All rules stay in Progression's Loadout;
    /// refusals come back through the status callback.
    /// </summary>
    public class LoadoutEditor
    {
        private static readonly EquipmentSlot[] WornSlots =
            { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Foci, EquipmentSlot.Shroud };

        // Affixes are hidden for First Playable (decided Nov 19) because they aren't fully built yet.
        // The data and Loadout rules are untouched: set this to true to bring the rows back.
        // (static readonly, not const, so the compiler doesn't warn that the affix code is unreachable)
        private static readonly bool ShowAffixes = false;

        private readonly RectTransform _list;
        private readonly Func<EquipmentSlot, List<EquipmentDefinition>> _getChoices;
        private readonly Action<string, bool> _setStatus;

        /// <param name="list">The container to draw rows into (it should have a vertical layout).</param>
        /// <param name="getChoices">The items allowed in a slot, e.g. CharacterCreator.GetEquipmentChoices.</param>
        /// <param name="setStatus">Shows a message on the owning screen; the bool is true for a warning.</param>
        public LoadoutEditor(RectTransform list, Func<EquipmentSlot, List<EquipmentDefinition>> getChoices, Action<string, bool> setStatus)
        {
            _list = list;
            _getChoices = getChoices;
            _setStatus = setStatus;
        }

        /// <summary>"Burden 1/2    Move speed 90%"</summary>
        public static string DescribeBurden(Loadout loadout)
        {
            return $"Burden {loadout.BurdenSteps}/{Loadout.MaxBurdenSteps}    Move speed {Mathf.RoundToInt(loadout.MoveSpeedMultiplier * 100f)}%";
        }

        /// <summary>Clears the list and draws every slot that has choices, then the affixes.</summary>
        public void Draw(Loadout loadout)
        {
            UIKit.ClearChildren(_list);

            foreach (EquipmentSlot slot in WornSlots)
            {
                if (_getChoices(slot).Count > 0)
                {
                    AddSlotRow(loadout, slot);
                }
            }

            if (!ShowAffixes)
            {
                return;
            }

            List<EquipmentDefinition> affixes = _getChoices(EquipmentSlot.Affix);
            if (affixes.Count > 0)
            {
                UIKit.CreateText(_list, "Affixes", 28f, TextAlignmentOptions.Left, UIKit.TextColor);
                foreach (EquipmentDefinition affix in affixes)
                {
                    AddAffixRow(loadout, (AffixDefinition)affix);
                }
            }
        }

        // One row per slot: "< Sword >" cycles through this slot's items plus "(none)".
        private void AddSlotRow(Loadout loadout, EquipmentSlot slot)
        {
            EquipmentDefinition equipped = loadout.GetEquipped(slot);
            string itemName = equipped != null ? equipped.DisplayName : "(none)";

            RectTransform row = UIKit.CreateGroup(_list, slot + " Row");
            UIKit.AddHorizontalLayout(row, 8f, 0);
            UIKit.SetSize(row, 56f);

            Button previous = UIKit.CreateButton(row, "<", () => CycleSlot(loadout, slot, -1), 56f);
            UIKit.SetSize(previous, 56f, 56f).flexibleWidth = 0f;
            TextMeshProUGUI slotLabel = UIKit.CreateText(row, $"{slot}: {itemName}", 26f, TextAlignmentOptions.Center, UIKit.TextColor);
            UIKit.FitText(slotLabel, 16f);
            Button next = UIKit.CreateButton(row, ">", () => CycleSlot(loadout, slot, 1), 56f);
            UIKit.SetSize(next, 56f, 56f).flexibleWidth = 0f;

            if (equipped != null && !string.IsNullOrEmpty(equipped.Description))
            {
                UIKit.CreateText(_list, equipped.Description, 20f, TextAlignmentOptions.Left, UIKit.MutedText);
            }
        }

        private void AddAffixRow(Loadout loadout, AffixDefinition affix)
        {
            string state = "not attuned";
            foreach (EquippedAffix attuned in loadout.Affixes)
            {
                if (attuned.Affix == affix)
                {
                    state = attuned.Form.ToString();
                }
            }

            RectTransform row = UIKit.CreateGroup(_list, affix.DisplayName + " Row");
            UIKit.AddHorizontalLayout(row, 8f, 0);
            UIKit.SetSize(row, 52f);

            TextMeshProUGUI affixLabel = UIKit.CreateText(row, $"{affix.DisplayName}: {state}", 24f, TextAlignmentOptions.MidlineLeft, UIKit.TextColor);
            UIKit.FitText(affixLabel, 16f);
            UIKit.CreateButton(row, "Offensive", () => AttuneAffix(loadout, affix, AffixForm.Offensive), 52f);
            UIKit.CreateButton(row, "Defensive", () => AttuneAffix(loadout, affix, AffixForm.Defensive), 52f);
            UIKit.CreateButton(row, "Remove", () => RemoveAffix(loadout, affix), 52f);
        }

        private void CycleSlot(Loadout loadout, EquipmentSlot slot, int direction)
        {
            // Choices are this slot's items plus null for "(none)"; step through them and wrap around.
            List<EquipmentDefinition> choices = new List<EquipmentDefinition> { null };
            choices.AddRange(_getChoices(slot));

            int index = choices.IndexOf(loadout.GetEquipped(slot));
            int nextIndex = (index + direction + choices.Count) % choices.Count;
            EquipmentDefinition next = choices[nextIndex];

            string reason;
            bool changed = next == null ? loadout.TryUnequip(slot, out reason) : loadout.TryEquip(next, out reason);
            _setStatus(changed ? string.Empty : reason, !changed);
        }

        private void AttuneAffix(Loadout loadout, AffixDefinition affix, AffixForm form)
        {
            // Switching form = remove, then add. If the add is refused, put the old form back.
            AffixForm? oldForm = null;
            foreach (EquippedAffix attuned in loadout.Affixes)
            {
                if (attuned.Affix == affix)
                {
                    oldForm = attuned.Form;
                }
            }

            if (oldForm == form)
            {
                return;
            }

            if (oldForm.HasValue)
            {
                loadout.TryRemoveAffix(affix, out _);
            }

            if (loadout.TryAddAffix(affix, form, out string reason))
            {
                _setStatus(string.Empty, false);
                return;
            }

            if (oldForm.HasValue)
            {
                loadout.TryAddAffix(affix, oldForm.Value, out _);
            }

            _setStatus(reason, true);
        }

        private void RemoveAffix(Loadout loadout, AffixDefinition affix)
        {
            bool removed = loadout.TryRemoveAffix(affix, out string reason);
            _setStatus(removed ? string.Empty : reason, !removed);
        }
    }
}
