using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for granting abilities to the player (Trello: "Skill tree system").</summary>
    public class AbilityGranterTests
    {
        private AbilityDefinition _slash;
        private AbilityDefinition _whirlwind;
        private AbilityDefinition _blender;
        private AbilityDefinition _limber;
        private WeaponDefinition _bladedSword;
        private SkillTreeDefinition _bladedTree;
        private SkillTreeDefinition _openTree;
        private CharacterProgress _progress;
        private Loadout _loadout;
        private CharacterSkills _skills;
        private FakeAbilityUser _user;

        [SetUp]
        public void SetUp()
        {
            _slash = TestSkillTrees.Ability("Slash");
            _whirlwind = TestSkillTrees.Ability("Whirlwind");
            _blender = TestSkillTrees.Ability("Blender");
            _limber = TestSkillTrees.Ability("Limber");

            _bladedSword = ScriptableObject.CreateInstance<WeaponDefinition>();
            SerializedObject sword = new SerializedObject(_bladedSword);
            SerializedProperty tags = sword.FindProperty("_tags");
            tags.arraySize = 1;
            tags.GetArrayElementAtIndex(0).enumValueIndex = (int)EquipmentTag.Bladed;
            SerializedProperty abilities = sword.FindProperty("_grantedAbilities");
            abilities.arraySize = 1;
            abilities.GetArrayElementAtIndex(0).objectReferenceValue = _slash;
            sword.ApplyModifiedPropertiesWithoutUndo();

            _bladedTree = TestSkillTrees.Create(
                Archetype.Martial,
                TestSkillTrees.Node(_whirlwind),
                TestSkillTrees.Upgrade(_blender, _whirlwind));
            SerializedObject tree = new SerializedObject(_bladedTree);
            tree.FindProperty("_requiresEquipment").boolValue = true;
            tree.FindProperty("_requiredTag").enumValueIndex = (int)EquipmentTag.Bladed;
            tree.ApplyModifiedPropertiesWithoutUndo();

            _openTree = TestSkillTrees.Create(Archetype.Martial, TestSkillTrees.Node(_limber));

            _progress = new CharacterProgress(ScriptableObject.CreateInstance<LevelCurveDefinition>());
            _progress.AddXp(_progress.XpToNextLevel); // level 2: 2 skill points
            _loadout = new Loadout(new CharacterStats(Archetype.Martial));
            _skills = new CharacterSkills(Archetype.Martial, _progress);
            _user = new FakeAbilityUser();
        }

        [Test]
        public void WornEquipment_IsGrantedOnCreation()
        {
            _loadout.TryEquip(_bladedSword, out _);

            new AbilityGranter(_loadout, _skills, _user);

            Assert.IsTrue(_user.HasAbility(_slash));
        }

        [Test]
        public void BuyingANode_GrantsItsAbility()
        {
            new AbilityGranter(_loadout, _skills, _user);

            _skills.TryUnlockNextNode(_openTree, null, out _);

            Assert.IsTrue(_user.HasAbility(_limber));
        }

        [Test]
        public void TreeBoughtWithoutItsGear_IsInactive_UntilTheGearIsWorn()
        {
            new AbilityGranter(_loadout, _skills, _user);

            Assert.IsTrue(_skills.TryUnlockNextNode(_bladedTree, null, out string reason), reason);
            Assert.IsFalse(_user.HasAbility(_whirlwind));

            _loadout.TryEquip(_bladedSword, out _);
            Assert.IsTrue(_user.HasAbility(_whirlwind));
        }

        // Acceptance test (Trello): does removing a bladed weapon disable Bladed Weapons abilities without refunding them?
        [Test]
        public void RemovingTheBladedWeapon_RevokesTreeAbilities_WithoutRefund()
        {
            _loadout.TryEquip(_bladedSword, out _);
            new AbilityGranter(_loadout, _skills, _user);
            _skills.TryUnlockNextNode(_bladedTree, null, out _);
            int pointsBefore = _progress.UnspentSkillPoints;

            _loadout.TryUnequip(EquipmentSlot.Weapon, out _);

            Assert.IsFalse(_user.HasAbility(_whirlwind));
            Assert.IsFalse(_user.HasAbility(_slash));
            Assert.AreEqual(1, _skills.GetUnlockedCount(_bladedTree));
            Assert.AreEqual(pointsBefore, _progress.UnspentSkillPoints);
        }

        [Test]
        public void BuyingAnUpgrade_RevokesTheOldAbility_AndGrantsTheNewOne()
        {
            _loadout.TryEquip(_bladedSword, out _);
            new AbilityGranter(_loadout, _skills, _user);

            _skills.TryUnlockNextNode(_bladedTree, null, out _);
            _skills.TryUnlockNextNode(_bladedTree, null, out _);

            Assert.IsFalse(_user.HasAbility(_whirlwind));
            Assert.IsTrue(_user.HasAbility(_blender));
        }

        [Test]
        public void Refresh_OnlySendsChanges()
        {
            _loadout.TryEquip(_bladedSword, out _);
            AbilityGranter granter = new AbilityGranter(_loadout, _skills, _user);
            int grantsBefore = _user.GrantCalls;

            granter.Refresh();
            granter.Refresh();

            Assert.AreEqual(grantsBefore, _user.GrantCalls);
            Assert.AreEqual(0, _user.RevokeCalls);
        }

        [Test]
        public void AfterDispose_ChangesAreIgnored()
        {
            AbilityGranter granter = new AbilityGranter(_loadout, _skills, _user);
            granter.Dispose();

            _loadout.TryEquip(_bladedSword, out _);

            Assert.IsFalse(_user.HasAbility(_slash));
        }

        /// <summary>
        /// Stand-in for Player's ability runner: just a list, plus counters so tests can check
        /// that only changes are sent.
        /// </summary>
        private class FakeAbilityUser : IAbilityUser
        {
            private readonly List<AbilityDefinition> _abilities = new List<AbilityDefinition>();

            public int GrantCalls { get; private set; }

            public int RevokeCalls { get; private set; }

            public void GrantAbility(AbilityDefinition ability)
            {
                GrantCalls++;
                if (!_abilities.Contains(ability))
                {
                    _abilities.Add(ability);
                }
            }

            public void RevokeAbility(AbilityDefinition ability)
            {
                RevokeCalls++;
                _abilities.Remove(ability);
            }

            public bool HasAbility(AbilityDefinition ability)
            {
                return _abilities.Contains(ability);
            }
        }
    }
}
