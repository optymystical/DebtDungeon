using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Checks the real preset assets in Data/Presets (Trello: "Presets").</summary>
    public class PresetTests
    {
        private const string PresetsFolder = "Assets/_Project/Data/Presets";
        private const int BaseMaxHealth = 50;
        private const int HealthPerPoint = 5;

        // Acceptance test (Trello): does each preset load with the right stats and gear?
        [TestCase("Preset_Bladesworn", Archetype.Martial, 10, 20, 10, "sword")]
        [TestCase("Preset_Vanguard", Archetype.Martial, 15, 5, 20, "greatsword", "leather_coat")]
        [TestCase("Preset_Stormcaller", Archetype.Magic, 10, 20, 10, "wand", "bastion")]
        [TestCase("Preset_Warden", Archetype.Magic, 15, 5, 20, "scroll", "bastion")]
        public void Preset_LoadsWithRightStatsAndGear(
            string assetName,
            Archetype archetype,
            int firstAttack,
            int secondAttack,
            int defensive,
            params string[] gearIds)
        {
            PresetDefinition preset = LoadPreset(assetName);
            CharacterStats stats = preset.CreateStats();
            Loadout loadout = new Loadout(stats);

            Assert.AreEqual(archetype, stats.Archetype);
            int firstStat = (int)archetype * StatRules.StatsPerArchetype;
            Assert.AreEqual(firstAttack, stats.GetStat((StatType)firstStat));
            Assert.AreEqual(secondAttack, stats.GetStat((StatType)(firstStat + 1)));
            Assert.AreEqual(defensive, stats.GetStat((StatType)(firstStat + 2)));

            Assert.IsTrue(preset.TryApplyStartingLoadout(loadout), "A starting item was refused.");
            List<string> equippedIds = new List<string>();
            foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Foci, EquipmentSlot.Shroud })
            {
                EquipmentDefinition item = loadout.GetEquipped(slot);
                if (item != null)
                {
                    equippedIds.Add(item.Id);
                }
            }

            CollectionAssert.AreEquivalent(gearIds, equippedIds);
        }

        [Test]
        public void ThereAreFourPresets_TwoPerAlphaArchetype()
        {
            string[] guids = AssetDatabase.FindAssets("t:PresetDefinition", new[] { PresetsFolder });
            int martial = 0;
            int magic = 0;

            foreach (string guid in guids)
            {
                PresetDefinition preset = AssetDatabase.LoadAssetAtPath<PresetDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (preset.Archetype == Archetype.Martial)
                {
                    martial++;
                }
                else if (preset.Archetype == Archetype.Magic)
                {
                    magic++;
                }
            }

            Assert.AreEqual(2, martial);
            Assert.AreEqual(2, magic);
        }

        [Test]
        public void DefensePresets_StartWithMoreHealthThanOffense()
        {
            int bladesworn = PlayerStats.CalculateMaxHealth(LoadPreset("Preset_Bladesworn").CreateStats(), BaseMaxHealth, HealthPerPoint);
            int vanguard = PlayerStats.CalculateMaxHealth(LoadPreset("Preset_Vanguard").CreateStats(), BaseMaxHealth, HealthPerPoint);

            Assert.AreEqual(100, bladesworn);
            Assert.AreEqual(150, vanguard);
        }

        private static PresetDefinition LoadPreset(string assetName)
        {
            PresetDefinition preset = AssetDatabase.LoadAssetAtPath<PresetDefinition>($"{PresetsFolder}/{assetName}.asset");
            Assert.IsNotNull(preset, $"Missing {assetName}.asset in {PresetsFolder}");
            return preset;
        }
    }
}
