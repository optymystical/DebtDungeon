using System;
using System.Collections.Generic;
using DebtDungeon.Progression;
using NUnit.Framework;
using UnityEditor;

namespace DebtDungeon.Game.Tests
{
    /// <summary>Edit Mode tests for carrying the character between scenes (Trello: "Scene flow and bootstrap").</summary>
    public class RunSessionTests
    {
        private const string PresetPath = "Assets/_Project/Data/Presets/Preset_Bladesworn.asset";
        private const string CurvePath = "Assets/_Project/Data/Progression/LevelCurve_Default.asset";

        [SetUp]
        public void SetUp()
        {
            RunSession.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            RunSession.Clear();
        }

        [Test]
        public void WithoutABlueprint_NoCharacterIsBuilt()
        {
            Assert.IsFalse(RunSession.HasBlueprint);
            Assert.IsNull(RunSession.CreateCharacterForRun());
        }

        // Acceptance test (Trello, Game Manager card): does Retry start a fresh run?
        [Test]
        public void EachRun_GetsAFreshLevelOneCharacter()
        {
            RunSession.SetBlueprint(CreateBlueprint("Ada"));

            CharacterData firstRun = RunSession.CreateCharacterForRun();
            firstRun.Progress.AddXp(500);
            CharacterData retry = RunSession.CreateCharacterForRun();

            Assert.AreNotSame(firstRun, retry);
            Assert.AreEqual(1, retry.Progress.Level);
            Assert.AreEqual("Ada", retry.Name);
        }

        [Test]
        public void SettingNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RunSession.SetBlueprint(null));
        }

        private static CharacterBlueprint CreateBlueprint(string characterName)
        {
            PresetDefinition preset = AssetDatabase.LoadAssetAtPath<PresetDefinition>(PresetPath);
            LevelCurveDefinition curve = AssetDatabase.LoadAssetAtPath<LevelCurveDefinition>(CurvePath);
            Assert.IsNotNull(preset, $"Missing {PresetPath}");
            Assert.IsNotNull(curve, $"Missing {CurvePath}");
            return new CharacterBlueprint(preset, characterName, new List<EquipmentDefinition>(preset.StartingEquipment), new List<EquippedAffix>(), curve);
        }
    }
}
