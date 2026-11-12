using DebtDungeon.Core;
using NUnit.Framework;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>
    /// Edit Mode tests for CharacterStats (Trello: "Stat system with archetype filter").
    /// Run them from Window > General > Test Runner > EditMode > Run All.
    /// </summary>
    public class CharacterStatsTests
    {
        private readonly object _testSource = new object();

        // Acceptance test 1 (Trello): does a Martial character ignore a Reach modifier?
        [Test]
        public void MartialCharacter_IgnoresReachModifier()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            stats.SetBaseStat(StatType.Strength, 10);

            stats.AddModifier(new StatModifier(StatType.Reach, 5, _testSource));

            Assert.AreEqual(0, stats.GetStat(StatType.Reach));
            Assert.AreEqual(10, stats.GetStat(StatType.Strength));
        }

        // Acceptance test 2 (Trello): does adding and removing a modifier return the stat to its original value?
        [Test]
        public void AddingThenRemovingModifier_RestoresOriginalValue()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            stats.SetBaseStat(StatType.Strength, 10);

            stats.AddModifier(new StatModifier(StatType.Strength, 3, _testSource));
            Assert.AreEqual(13, stats.GetStat(StatType.Strength));

            stats.RemoveModifiersFromSource(_testSource);
            Assert.AreEqual(10, stats.GetStat(StatType.Strength));
        }

        [Test]
        public void OffArchetypeBaseStat_IsIgnored()
        {
            CharacterStats stats = new CharacterStats(Archetype.Magic);

            stats.SetBaseStat(StatType.Strength, 20);

            Assert.AreEqual(0, stats.GetStat(StatType.Strength));
        }

        [Test]
        public void RemovingOneSource_KeepsOtherSourcesModifiers()
        {
            CharacterStats stats = new CharacterStats(Archetype.Magic);
            object weapon = new object();
            object skill = new object();
            stats.SetBaseStat(StatType.Build, 10);

            stats.AddModifier(new StatModifier(StatType.Build, 2, weapon));
            stats.AddModifier(new StatModifier(StatType.Build, 5, skill));
            stats.RemoveModifiersFromSource(weapon);

            Assert.AreEqual(15, stats.GetStat(StatType.Build));
        }

        [Test]
        public void Power_IsStatDividedByTen()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            stats.SetBaseStat(StatType.Dexterity, 15);

            Assert.AreEqual(1.5f, stats.GetPower(StatType.Dexterity), 0.0001f);
        }

        [Test]
        public void LargePenalty_StopsAtZero()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            stats.SetBaseStat(StatType.Constitution, 5);

            stats.AddModifier(new StatModifier(StatType.Constitution, -20, _testSource));

            Assert.AreEqual(0, stats.GetStat(StatType.Constitution));
        }

        [Test]
        public void StatsChanged_FiresOnBaseAndModifierChanges()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            int timesRaised = 0;
            stats.StatsChanged += () => timesRaised++;

            stats.SetBaseStat(StatType.Strength, 10);
            stats.AddModifier(new StatModifier(StatType.Strength, 2, _testSource));
            stats.RemoveModifiersFromSource(_testSource);

            Assert.AreEqual(3, timesRaised);
        }
    }
}
