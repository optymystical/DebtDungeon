using DebtDungeon.Core;
using NUnit.Framework;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Armor and Shroud Durability/Limit scale with the defensive stat's Power (decided Nov 17).</summary>
    public class DefenseScalingTests
    {
        // Default asset values: armor Durability 40 / Limit 8; Shroud 100 / 20, regen 20 per second after 2.5 seconds.

        [Test]
        public void Armor_AtPowerTwo_DoublesDurabilityAndLimit()
        {
            ArmorAbsorber armor = new ArmorAbsorber(ScriptableObject.CreateInstance<ArmorDefinition>());

            armor.ApplyStatPower(2f);

            Assert.AreEqual(80, armor.MaxDurability);
            Assert.AreEqual(80, armor.CurrentDurability);
            Assert.AreEqual(16, armor.Limit);
        }

        [Test]
        public void LowStat_NeverGoesBelowListedValues()
        {
            ArmorAbsorber armor = new ArmorAbsorber(ScriptableObject.CreateInstance<ArmorDefinition>());

            armor.ApplyStatPower(0.5f);

            Assert.AreEqual(40, armor.MaxDurability);
            Assert.AreEqual(8, armor.Limit);
        }

        [Test]
        public void Rescale_KeepsTheDurabilityPercentage()
        {
            ArmorAbsorber armor = new ArmorAbsorber(ScriptableObject.CreateInstance<ArmorDefinition>());
            armor.AbsorbDamage(new DamageInfo(8, DamageType.Physical, null, Faction.Goblin, false));
            armor.AbsorbDamage(new DamageInfo(8, DamageType.Physical, null, Faction.Goblin, false)); // 24 / 40 = 60%

            armor.ApplyStatPower(2f);

            Assert.AreEqual(80, armor.MaxDurability);
            Assert.AreEqual(48, armor.CurrentDurability);
        }

        [Test]
        public void Rescale_IsWorkedOutFromListedValues_SoItCanGoBackDown()
        {
            ArmorAbsorber armor = new ArmorAbsorber(ScriptableObject.CreateInstance<ArmorDefinition>());

            armor.ApplyStatPower(3f);
            armor.ApplyStatPower(1.5f);

            Assert.AreEqual(60, armor.MaxDurability);
            Assert.AreEqual(12, armor.Limit);
        }

        [Test]
        public void Shroud_RegenScalesWithTheSameMultiplier()
        {
            ShroudAbsorber shroud = new ShroudAbsorber(ScriptableObject.CreateInstance<ShroudDefinition>());
            shroud.ApplyStatPower(2f);                                                            // 200 / 40, regen 40/s
            shroud.AbsorbDamage(new DamageInfo(40, DamageType.Magic, null, Faction.Spider, false)); // 160

            shroud.Tick(2.5f); // the delay passes, no regen yet
            shroud.Tick(1f);   // one second of regen at 40/s

            Assert.AreEqual(200, shroud.CurrentDurability);
        }

        [Test]
        public void ScalingStats_AreTheDefensiveStats()
        {
            Assert.AreEqual(StatType.Constitution, ScriptableObject.CreateInstance<ArmorDefinition>().ScalingStat);
            Assert.AreEqual(StatType.Build, ScriptableObject.CreateInstance<ShroudDefinition>().ScalingStat);
        }
    }
}
