using DebtDungeon.Core;
using NUnit.Framework;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for the max HP formula (Trello: "Max health from the defensive stat").</summary>
    public class MaxHealthTests
    {
        private const int BaseMaxHealth = 50;
        private const int HealthPerPoint = 5;

        // Acceptance test (Trello): does raising the defensive stat raise max HP as the formula predicts?
        [Test]
        public void RaisingDefensiveStat_RaisesMaxHealthByFormula()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            stats.SetBaseStat(StatType.Constitution, 10);
            Assert.AreEqual(100, PlayerStats.CalculateMaxHealth(stats, BaseMaxHealth, HealthPerPoint));

            stats.SetBaseStat(StatType.Constitution, 15);
            Assert.AreEqual(125, PlayerStats.CalculateMaxHealth(stats, BaseMaxHealth, HealthPerPoint));
        }

        [Test]
        public void MagicCharacter_UsesBuild()
        {
            CharacterStats stats = new CharacterStats(Archetype.Magic);
            stats.SetBaseStat(StatType.Build, 20);

            Assert.AreEqual(150, PlayerStats.CalculateMaxHealth(stats, BaseMaxHealth, HealthPerPoint));
        }

        [Test]
        public void AttackStat_DoesNotChangeMaxHealth()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            stats.SetBaseStat(StatType.Constitution, 10);
            stats.SetBaseStat(StatType.Strength, 30);

            Assert.AreEqual(100, PlayerStats.CalculateMaxHealth(stats, BaseMaxHealth, HealthPerPoint));
        }

        [Test]
        public void DefensiveModifier_CountsTowardMaxHealth()
        {
            CharacterStats stats = new CharacterStats(Archetype.Martial);
            object armor = new object();
            stats.SetBaseStat(StatType.Constitution, 10);

            stats.AddModifier(new StatModifier(StatType.Constitution, 4, armor));

            Assert.AreEqual(120, PlayerStats.CalculateMaxHealth(stats, BaseMaxHealth, HealthPerPoint));
        }
    }
}
