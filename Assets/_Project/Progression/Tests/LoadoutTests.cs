using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for Loadout (Trello: "Equipment data and slots").</summary>
    public class LoadoutTests
    {
        // Acceptance test (Trello): is a Magic character unable to equip a sword?
        [Test]
        public void MagicCharacter_CannotEquipSword()
        {
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Magic));
            WeaponDefinition sword = ScriptableObject.CreateInstance<WeaponDefinition>();

            bool equipped = loadout.TryEquip(sword, out string reason);

            Assert.IsFalse(equipped);
            Assert.IsNotNull(reason);
            Assert.IsNull(loadout.GetEquipped(EquipmentSlot.Weapon));
        }

        [Test]
        public void MediumArmor_CostsTwoBurdenSteps()
        {
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Martial));
            ArmorDefinition coat = ScriptableObject.CreateInstance<ArmorDefinition>(); // Medium by default

            loadout.TryEquip(coat, out _);

            Assert.AreEqual(2, loadout.BurdenSteps);
            Assert.AreEqual(0.8f, loadout.MoveSpeedMultiplier, 0.0001f);
        }

        [Test]
        public void AffixInFreeSlot_CostsNoBurden_ExtraAffixDoes()
        {
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Magic));
            ShroudDefinition bastion = ScriptableObject.CreateInstance<ShroudDefinition>(); // 1 free Defensive slot
            AffixDefinition first = ScriptableObject.CreateInstance<AffixDefinition>();
            AffixDefinition second = ScriptableObject.CreateInstance<AffixDefinition>();
            loadout.TryEquip(bastion, out _);

            loadout.TryAddAffix(first, AffixForm.Defensive, out _);
            Assert.AreEqual(0, loadout.BurdenSteps);

            loadout.TryAddAffix(second, AffixForm.Defensive, out _);
            Assert.AreEqual(1, loadout.BurdenSteps);
        }

        [Test]
        public void GoingOverBurdenCap_IsRefused()
        {
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Magic));
            AffixDefinition[] gems = CreateAffixes(3);

            Assert.IsTrue(loadout.TryAddAffix(gems[0], AffixForm.Offensive, out _));
            Assert.IsTrue(loadout.TryAddAffix(gems[1], AffixForm.Offensive, out _));
            bool third = loadout.TryAddAffix(gems[2], AffixForm.Offensive, out string reason);

            Assert.IsFalse(third);
            Assert.IsNotNull(reason);
            Assert.AreEqual(2, loadout.BurdenSteps);
            Assert.AreEqual(2, loadout.Affixes.Count);
        }

        [Test]
        public void DuplicateAffix_IsRefused()
        {
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Magic));
            AffixDefinition agate = ScriptableObject.CreateInstance<AffixDefinition>();

            loadout.TryAddAffix(agate, AffixForm.Offensive, out _);
            bool again = loadout.TryAddAffix(agate, AffixForm.Defensive, out _);

            Assert.IsFalse(again);
            Assert.AreEqual(1, loadout.Affixes.Count);
        }

        [Test]
        public void StatBonus_AppliesWhileWorn_AndIsRemovedOnUnequip()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            stats.SetBaseStat(StatType.Dexterity, 10);
            Loadout loadout = new Loadout(stats);
            WeaponDefinition sword = ScriptableObject.CreateInstance<WeaponDefinition>();
            AddStatBonus(sword, StatType.Dexterity, 2);

            loadout.TryEquip(sword, out _);
            Assert.AreEqual(12, stats.GetStat(StatType.Dexterity));

            loadout.TryUnequip(EquipmentSlot.Weapon, out _);
            Assert.AreEqual(10, stats.GetStat(StatType.Dexterity));
        }

        [Test]
        public void SwappingWeapons_ReplacesTheOldBonus()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            Loadout loadout = new Loadout(stats);
            WeaponDefinition sword = ScriptableObject.CreateInstance<WeaponDefinition>();
            WeaponDefinition greatsword = ScriptableObject.CreateInstance<WeaponDefinition>();
            AddStatBonus(sword, StatType.Dexterity, 2);
            AddStatBonus(greatsword, StatType.Strength, 3);

            loadout.TryEquip(sword, out _);
            loadout.TryEquip(greatsword, out _);

            Assert.AreEqual(0, stats.GetStat(StatType.Dexterity));
            Assert.AreEqual(3, stats.GetStat(StatType.Strength));
            Assert.AreEqual(greatsword, loadout.GetEquipped(EquipmentSlot.Weapon));
        }

        [Test]
        public void TagFromWornGear_IsVisibleToSkillTrees()
        {
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Martial));
            WeaponDefinition sword = ScriptableObject.CreateInstance<WeaponDefinition>();
            AddTag(sword, EquipmentTag.Bladed);

            Assert.IsFalse(loadout.HasTag(EquipmentTag.Bladed));
            loadout.TryEquip(sword, out _);
            Assert.IsTrue(loadout.HasTag(EquipmentTag.Bladed));
        }

        private static AffixDefinition[] CreateAffixes(int count)
        {
            AffixDefinition[] affixes = new AffixDefinition[count];
            for (int i = 0; i < count; i++)
            {
                affixes[i] = ScriptableObject.CreateInstance<AffixDefinition>();
            }

            return affixes;
        }

        // The fields are private (Inspector-only), so tests set them the way the Inspector does: through SerializedObject.
        private static void AddStatBonus(EquipmentDefinition item, StatType stat, int value)
        {
            SerializedObject serialized = new SerializedObject(item);
            SerializedProperty bonuses = serialized.FindProperty("_statBonuses");
            bonuses.arraySize++;
            SerializedProperty bonus = bonuses.GetArrayElementAtIndex(bonuses.arraySize - 1);
            bonus.FindPropertyRelative("_stat").enumValueIndex = (int)stat;
            bonus.FindPropertyRelative("_value").intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddTag(EquipmentDefinition item, EquipmentTag tag)
        {
            SerializedObject serialized = new SerializedObject(item);
            SerializedProperty tags = serialized.FindProperty("_tags");
            tags.arraySize++;
            tags.GetArrayElementAtIndex(tags.arraySize - 1).enumValueIndex = (int)tag;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
