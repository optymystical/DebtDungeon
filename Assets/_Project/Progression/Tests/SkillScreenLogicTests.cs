using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for what the skill and stat screen shows (Trello: "Skill tree and stat-point UI").</summary>
    public class SkillScreenLogicTests
    {
        private AbilityDefinition _whirlwind;
        private AbilityDefinition _blender;
        private AbilityDefinition _closeQuarters;
        private SkillTreeDefinition _bladedTree;
        private CharacterProgress _progress;
        private CharacterSkills _skills;

        [SetUp]
        public void SetUp()
        {
            _whirlwind = TestSkillTrees.Ability("Whirlwind");
            _blender = TestSkillTrees.Ability("Blender");
            _closeQuarters = TestSkillTrees.Ability("Close Quarters");
            _bladedTree = TestSkillTrees.Create(
                Archetype.Martial,
                TestSkillTrees.Node(_whirlwind),
                TestSkillTrees.Node(_closeQuarters),
                TestSkillTrees.Upgrade(_blender, _whirlwind));
            SerializedObject tree = new SerializedObject(_bladedTree);
            tree.FindProperty("_requiresEquipment").boolValue = true;
            tree.FindProperty("_requiredTag").enumValueIndex = (int)EquipmentTag.Bladed;
            tree.ApplyModifiedPropertiesWithoutUndo();

            _progress = new CharacterProgress(ScriptableObject.CreateInstance<LevelCurveDefinition>());
            _skills = new CharacterSkills(Archetype.Martial, _progress);
        }

        [Test]
        public void NodeStates_AreOwnedThenNextThenLocked()
        {
            _skills.TryUnlockNextNode(_bladedTree, null, out _);

            Assert.AreEqual(SkillNodeState.Owned, _skills.GetNodeState(_bladedTree, 0));
            Assert.AreEqual(SkillNodeState.Next, _skills.GetNodeState(_bladedTree, 1));
            Assert.AreEqual(SkillNodeState.Locked, _skills.GetNodeState(_bladedTree, 2));
        }

        [Test]
        public void Notes_DescribeGearAndUpgrades()
        {
            Assert.AreEqual("Needs a Bladed weapon", _bladedTree.GetEquipmentNote());
            Assert.IsNull(_bladedTree.Nodes[0].GetUpgradeNote());
            Assert.AreEqual("Upgrades Whirlwind", _bladedTree.Nodes[2].GetUpgradeNote());
            Assert.IsNull(TestSkillTrees.Create(Archetype.Magic, TestSkillTrees.Node(_whirlwind)).GetEquipmentNote());
        }

        [Test]
        public void OwnedAbilities_WithoutTheGear_AreListedAsInactive_WithTheReason()
        {
            _skills.TryUnlockNextNode(_bladedTree, null, out _);
            Loadout noWeapon = new Loadout(new CharacterStats(Archetype.Martial));

            List<OwnedSkillAbility> rows = _skills.GetOwnedAbilities(noWeapon);

            Assert.AreEqual(1, rows.Count);
            Assert.AreSame(_whirlwind, rows[0].Ability);
            Assert.IsFalse(rows[0].IsActive);
            Assert.AreEqual("Needs a Bladed weapon", rows[0].InactiveReason);
        }

        [Test]
        public void OwnedAbilities_WithTheGear_AreActive()
        {
            _skills.TryUnlockNextNode(_bladedTree, null, out _);
            Loadout loadout = new Loadout(new CharacterStats(Archetype.Martial));
            WeaponDefinition sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/_Project/Data/Items/Weapon_Sword.asset");
            Assert.IsTrue(loadout.TryEquip(sword, out string reason), reason);

            List<OwnedSkillAbility> rows = _skills.GetOwnedAbilities(loadout);

            Assert.IsTrue(rows[0].IsActive);
            Assert.IsNull(rows[0].InactiveReason);
        }

        [Test]
        public void CanSpendStatPoint_ChecksWithoutSpending()
        {
            CharacterData character = CharacterData.CreateFromPreset(
                AssetDatabase.LoadAssetAtPath<PresetDefinition>("Assets/_Project/Data/Presets/Preset_Vanguard.asset"),
                "Test",
                ScriptableObject.CreateInstance<LevelCurveDefinition>());
            character.Progress.AddXp(50); // level 2: 3 stat points

            Assert.IsTrue(character.CanSpendStatPoint(StatType.Strength, null, out _));
            Assert.AreEqual(3, character.Progress.UnspentStatPoints);
            Assert.IsFalse(character.CanSpendStatPoint(StatType.Reach, null, out string reason));
            Assert.IsNotNull(reason);
        }

        [Test]
        public void ArchetypeStats_ListsTheThreeOwnStatsInOrder()
        {
            List<StatType> martial = ArchetypeStats.GetStats(Archetype.Martial);

            Assert.AreEqual(3, martial.Count);
            Assert.AreEqual(StatType.Strength, martial[0]);
            Assert.AreEqual(StatType.Constitution, martial[2]);
        }
    }
}
