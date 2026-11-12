using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Checks the real starting-item assets in Data/Items (Trello: "Does every starting item load from data?").</summary>
    public class StartingItemTests
    {
        private const string ItemsFolder = "Assets/_Project/Data/Items";

        // Acceptance test (Trello): does every starting item load from data?
        [Test]
        public void EveryStartingItem_LoadsWithIdAndName()
        {
            List<EquipmentDefinition> items = LoadAllItems();
            HashSet<string> ids = new HashSet<string>();

            Assert.AreEqual(9, items.Count, "Expected 2 weapons, 1 armor, 2 Foci, 1 Shroud and 3 Affixes.");
            foreach (EquipmentDefinition item in items)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.Id), $"{item.name} has no id.");
                Assert.IsFalse(string.IsNullOrEmpty(item.DisplayName), $"{item.name} has no display name.");
                Assert.IsTrue(ids.Add(item.Id), $"Duplicate id: {item.Id}");
            }
        }

        [Test]
        public void Sword_IsLightBladedMartialGear()
        {
            WeaponDefinition sword = Load<WeaponDefinition>("Weapon_Sword");

            Assert.AreEqual(AttackWeight.Light, sword.Weight);
            Assert.AreEqual(StatType.Dexterity, sword.ScalingStat);
            Assert.AreEqual(Archetype.Martial, sword.Archetype);
            Assert.IsTrue(sword.HasTag(EquipmentTag.Bladed));
        }

        [Test]
        public void LeatherCoat_IsMediumArmor()
        {
            ArmorDefinition coat = Load<ArmorDefinition>("Armor_LeatherCoat");

            Assert.AreEqual(2, coat.BurdenSteps);
            Assert.AreEqual(40, coat.Durability);
            Assert.AreEqual(8, coat.Limit);
            Assert.IsTrue(coat.HasTag(EquipmentTag.MediumArmor));
        }

        [Test]
        public void ScrollAndWand_ScaleWithDifferentStats()
        {
            FociDefinition scroll = Load<FociDefinition>("Foci_Scroll");
            FociDefinition wand = Load<FociDefinition>("Foci_Wand");

            Assert.AreEqual(StatType.Reach, scroll.ScalingStat);
            Assert.AreEqual(AttackWeight.Heavy, scroll.Weight);
            Assert.AreEqual(2, scroll.FreeOffensiveSlots);
            Assert.AreEqual(StatType.Grasp, wand.ScalingStat);
            Assert.AreEqual(AttackWeight.Light, wand.Weight);
        }

        [Test]
        public void MagicCharacter_CanWearAFullStarterLoadout()
        {
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Magic));

            Assert.IsTrue(loadout.TryEquip(Load<FociDefinition>("Foci_Scroll"), out _));
            Assert.IsTrue(loadout.TryEquip(Load<ShroudDefinition>("Shroud_Bastion"), out _));
            Assert.IsTrue(loadout.TryAddAffix(Load<AffixDefinition>("Affix_Agate"), AffixForm.Defensive, out _));
            Assert.AreEqual(0, loadout.BurdenSteps);
        }

        private static List<EquipmentDefinition> LoadAllItems()
        {
            List<EquipmentDefinition> items = new List<EquipmentDefinition>();
            string[] guids = AssetDatabase.FindAssets("t:EquipmentDefinition", new[] { ItemsFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                items.Add(AssetDatabase.LoadAssetAtPath<EquipmentDefinition>(path));
            }

            return items;
        }

        private static T Load<T>(string assetName) where T : EquipmentDefinition
        {
            T item = AssetDatabase.LoadAssetAtPath<T>($"{ItemsFolder}/{assetName}.asset");
            Assert.IsNotNull(item, $"Missing {assetName}.asset in {ItemsFolder}");
            return item;
        }
    }
}
