using DebtDungeon.Core;
using NUnit.Framework;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for spending stat points (Trello: "XP and leveling").</summary>
    public class StatPointTests
    {
        private const string PresetPath = "Assets/_Project/Data/Presets/Preset_Vanguard.asset";

        [Test]
        public void OutOfCombat_SpendingAPoint_RaisesTheStat()
        {
            CharacterData character = CreateLeveledVanguard();
            int before = character.Stats.GetStat(StatType.Strength);

            bool spent = character.TrySpendStatPoint(StatType.Strength, new FakeCombatState(false), out _);

            Assert.IsTrue(spent);
            Assert.AreEqual(before + 1, character.Stats.GetStat(StatType.Strength));
            Assert.AreEqual(2, character.Progress.UnspentStatPoints);
        }

        [Test]
        public void InCombat_SpendingIsRefused_AndThePointIsKept()
        {
            CharacterData character = CreateLeveledVanguard();

            bool spent = character.TrySpendStatPoint(StatType.Strength, new FakeCombatState(true), out string reason);

            Assert.IsFalse(spent);
            Assert.IsNotNull(reason);
            Assert.AreEqual(3, character.Progress.UnspentStatPoints);
        }

        [Test]
        public void OtherArchetypesStat_IsRefused_AndThePointIsKept()
        {
            CharacterData character = CreateLeveledVanguard();

            bool spent = character.TrySpendStatPoint(StatType.Reach, null, out _);

            Assert.IsFalse(spent);
            Assert.AreEqual(3, character.Progress.UnspentStatPoints);
        }

        [Test]
        public void WithNoPoints_SpendingIsRefused()
        {
            CharacterData character = CharacterData.CreateFromPreset(LoadVanguard(), "Test", Curve());

            Assert.IsFalse(character.TrySpendStatPoint(StatType.Strength, null, out _));
        }

        [Test]
        public void SpendingOnConstitution_RaisesMaxHealthByTheFormula()
        {
            CharacterData character = CreateLeveledVanguard(); // Constitution 20 -> 150 HP

            character.TrySpendStatPoint(StatType.Constitution, null, out _);

            Assert.AreEqual(155, PlayerStats.CalculateMaxHealth(character.Stats, 50, 5));
        }

        [Test]
        public void BlankName_BecomesUnnamedDebtor()
        {
            CharacterData character = CharacterData.CreateFromPreset(LoadVanguard(), "   ", Curve());

            Assert.AreEqual("Unnamed Debtor", character.Name);
        }

        private static CharacterData CreateLeveledVanguard()
        {
            CharacterData character = CharacterData.CreateFromPreset(LoadVanguard(), "Test", Curve());
            character.Progress.AddXp(50); // level 2: 3 stat points
            return character;
        }

        private static PresetDefinition LoadVanguard()
        {
            PresetDefinition preset = UnityEditor.AssetDatabase.LoadAssetAtPath<PresetDefinition>(PresetPath);
            Assert.IsNotNull(preset, $"Missing {PresetPath}");
            return preset;
        }

        private static LevelCurveDefinition Curve()
        {
            return ScriptableObject.CreateInstance<LevelCurveDefinition>();
        }

        /// <summary>Stand-in for Player's combat state, so tests can switch "in combat" on and off.</summary>
        private class FakeCombatState : ICombatState
        {
            public FakeCombatState(bool inCombat)
            {
                InCombat = inCombat;
            }

            public bool InCombat { get; }
        }
    }
}
