using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for buying skill tree nodes (Trello: "Skill tree system").</summary>
    public class SkillUnlockTests
    {
        private AbilityDefinition _crackle;
        private AbilityDefinition _crackleII;
        private AbilityDefinition _lightningRod;
        private SkillTreeDefinition _magicTree;
        private CharacterProgress _progress;
        private CharacterSkills _skills;

        [SetUp]
        public void SetUp()
        {
            _crackle = TestSkillTrees.Ability("Crackle");
            _crackleII = TestSkillTrees.Ability("Crackle II");
            _lightningRod = TestSkillTrees.Ability("Lightning Rod");
            _magicTree = TestSkillTrees.Create(
                Archetype.Magic,
                TestSkillTrees.Node(_crackle),
                TestSkillTrees.Upgrade(_crackleII, _crackle),
                TestSkillTrees.Node(_lightningRod));

            // A fresh level-1 character starts with 1 skill point (LevelCurveDefinition default).
            _progress = new CharacterProgress(ScriptableObject.CreateInstance<LevelCurveDefinition>());
            _skills = new CharacterSkills(Archetype.Magic, _progress);
        }

        [Test]
        public void Unlocking_SpendsAPoint_AndGrantsTheFirstNode()
        {
            Assert.IsTrue(_skills.TryUnlockNextNode(_magicTree, null, out string reason), reason);

            Assert.AreEqual(1, _skills.GetUnlockedCount(_magicTree));
            Assert.AreEqual(0, _progress.UnspentSkillPoints);
            Assert.AreSame(_crackle, _skills.GetOwnedAbilities(_magicTree)[0]);
        }

        // Acceptance test (Trello): is node 3 unbuyable before node 2?
        [Test]
        public void SecondPurchase_BuysNode2_NotNode3()
        {
            GiveSkillPoints(2);

            _skills.TryUnlockNextNode(_magicTree, null, out _);
            _skills.TryUnlockNextNode(_magicTree, null, out _);

            List<AbilityDefinition> owned = _skills.GetOwnedAbilities(_magicTree);
            Assert.AreEqual(2, _skills.GetUnlockedCount(_magicTree));
            Assert.IsTrue(owned.Contains(_crackleII));
            Assert.IsFalse(owned.Contains(_lightningRod));
        }

        // Acceptance test (Trello): does an upgrade node replace its earlier ability?
        [Test]
        public void BuyingTheUpgrade_ReplacesTheOwnedAbility()
        {
            GiveSkillPoints(1);

            _skills.TryUnlockNextNode(_magicTree, null, out _);
            _skills.TryUnlockNextNode(_magicTree, null, out _);

            List<AbilityDefinition> owned = _skills.GetOwnedAbilities(_magicTree);
            Assert.AreEqual(1, owned.Count);
            Assert.AreSame(_crackleII, owned[0]);
        }

        [Test]
        public void InCombat_IsRefused_AndThePointIsKept()
        {
            Assert.IsFalse(_skills.TryUnlockNextNode(_magicTree, new FakeCombatState(true), out string reason));

            Assert.IsNotNull(reason);
            Assert.AreEqual(1, _progress.UnspentSkillPoints);
            Assert.AreEqual(0, _skills.GetUnlockedCount(_magicTree));
        }

        [Test]
        public void OtherArchetypesTree_IsRefused()
        {
            SkillTreeDefinition martialTree = TestSkillTrees.Create(Archetype.Martial, TestSkillTrees.Node(_crackle));

            Assert.IsFalse(_skills.TryUnlockNextNode(martialTree, null, out _));
            Assert.AreEqual(1, _progress.UnspentSkillPoints);
        }

        [Test]
        public void CompleteTree_IsRefused_AndThePointIsKept()
        {
            GiveSkillPoints(3);
            for (int i = 0; i < _magicTree.NodeCount; i++)
            {
                _skills.TryUnlockNextNode(_magicTree, null, out _);
            }

            Assert.IsFalse(_skills.TryUnlockNextNode(_magicTree, null, out _));
            Assert.AreEqual(1, _progress.UnspentSkillPoints);
        }

        [Test]
        public void WithNoPoints_IsRefused()
        {
            _skills.TryUnlockNextNode(_magicTree, null, out _);

            Assert.IsFalse(_skills.TryUnlockNextNode(_magicTree, null, out string reason));
            Assert.AreEqual("No skill points to spend.", reason);
        }

        [Test]
        public void SkillsChanged_FiresOnPurchase_NotOnRefusal()
        {
            int raised = 0;
            _skills.SkillsChanged += () => raised++;

            _skills.TryUnlockNextNode(_magicTree, null, out _);
            _skills.TryUnlockNextNode(_magicTree, null, out _); // no points left: refused

            Assert.AreEqual(1, raised);
        }

        /// <summary>Levels the character up until it has this many extra skill points (1 per level).</summary>
        private void GiveSkillPoints(int extraPoints)
        {
            int target = _progress.UnspentSkillPoints + extraPoints;
            while (_progress.UnspentSkillPoints < target)
            {
                _progress.AddXp(_progress.XpToNextLevel);
            }
        }

        /// <summary>Stand-in for Player's combat state.</summary>
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
