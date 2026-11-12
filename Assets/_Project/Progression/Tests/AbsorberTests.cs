using DebtDungeon.Core;
using NUnit.Framework;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for armor and Shroud absorption (Trello: "Equipment data and slots").</summary>
    public class AbsorberTests
    {
        // Default asset values: armor Durability 40 / Limit 8; Shroud 100 / 20, regen 20 per second after 2.5 seconds.

        [Test]
        public void Armor_AbsorbsUpToLimit_AndLosesThatDurability()
        {
            ArmorAbsorber armor = new ArmorAbsorber(ScriptableObject.CreateInstance<ArmorDefinition>());

            int gotThrough = armor.AbsorbDamage(Hit(15, DamageType.Physical));

            Assert.AreEqual(7, gotThrough);
            Assert.AreEqual(32, armor.CurrentDurability);
        }

        [Test]
        public void Armor_IgnoresPoisonAndMagic()
        {
            ArmorAbsorber armor = new ArmorAbsorber(ScriptableObject.CreateInstance<ArmorDefinition>());

            Assert.AreEqual(10, armor.AbsorbDamage(Hit(10, DamageType.Poison)));
            Assert.AreEqual(10, armor.AbsorbDamage(Hit(10, DamageType.Magic)));
            Assert.AreEqual(40, armor.CurrentDurability);
        }

        [Test]
        public void Armor_BreaksAtZero_UntilRepaired()
        {
            ArmorAbsorber armor = new ArmorAbsorber(ScriptableObject.CreateInstance<ArmorDefinition>());
            for (int i = 0; i < 5; i++)
            {
                armor.AbsorbDamage(Hit(8, DamageType.Physical));
            }

            Assert.IsTrue(armor.IsDepleted);
            Assert.AreEqual(8, armor.AbsorbDamage(Hit(8, DamageType.Physical)));

            armor.Repair();
            Assert.AreEqual(40, armor.CurrentDurability);
        }

        [Test]
        public void Shroud_BlocksMagic_ButNotPoison()
        {
            ShroudAbsorber shroud = new ShroudAbsorber(ScriptableObject.CreateInstance<ShroudDefinition>());

            Assert.AreEqual(0, shroud.AbsorbDamage(Hit(15, DamageType.Magic)));
            Assert.AreEqual(15, shroud.AbsorbDamage(Hit(15, DamageType.Poison)));
        }

        [Test]
        public void Shroud_WaitsForDelay_ThenRegenerates()
        {
            ShroudAbsorber shroud = new ShroudAbsorber(ScriptableObject.CreateInstance<ShroudDefinition>());
            for (int i = 0; i < 3; i++)
            {
                shroud.AbsorbDamage(Hit(20, DamageType.Physical));
            }

            shroud.Tick(2f);
            Assert.AreEqual(40, shroud.CurrentDurability);

            shroud.Tick(0.5f);
            shroud.Tick(1f);
            Assert.AreEqual(60, shroud.CurrentDurability);
        }

        [Test]
        public void Shroud_DamageRestartsTheDelay()
        {
            ShroudAbsorber shroud = new ShroudAbsorber(ScriptableObject.CreateInstance<ShroudDefinition>());
            shroud.AbsorbDamage(Hit(20, DamageType.Physical));

            shroud.Tick(2f);
            shroud.AbsorbDamage(Hit(5, DamageType.Poison)); // poison isn't blocked, but it's still damage
            shroud.Tick(2f);

            Assert.AreEqual(80, shroud.CurrentDurability);
        }

        [Test]
        public void Shroud_RegenStopsAtMax()
        {
            ShroudAbsorber shroud = new ShroudAbsorber(ScriptableObject.CreateInstance<ShroudDefinition>());
            shroud.AbsorbDamage(Hit(5, DamageType.Physical));

            shroud.Tick(3f);
            shroud.Tick(10f);

            Assert.AreEqual(100, shroud.CurrentDurability);
        }

        private static DamageInfo Hit(int amount, DamageType type)
        {
            return new DamageInfo(amount, type, null, Faction.Goblin);
        }
    }
}
