using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Edit Mode tests for skill tree data (Trello: "Skill tree system").</summary>
    public class SkillTreeDefinitionTests
    {
        private AbilityDefinition _crackle;
        private AbilityDefinition _crackleII;
        private AbilityDefinition _lightningRod;

        [SetUp]
        public void SetUp()
        {
            _crackle = TestSkillTrees.Ability("Crackle");
            _crackleII = TestSkillTrees.Ability("Crackle II");
            _lightningRod = TestSkillTrees.Ability("Lightning Rod");
        }

        // Acceptance test (Trello): does an upgrade node replace its earlier ability?
        [Test]
        public void UpgradeNode_ReplacesTheEarlierAbility()
        {
            SkillTreeDefinition tree = CreateRadiationLikeTree();

            List<AbilityDefinition> afterTwo = tree.GetAbilities(2);

            Assert.AreEqual(1, afterTwo.Count);
            Assert.AreSame(_crackleII, afterTwo[0]);
        }

        [Test]
        public void GetAbilities_GrowsNodeByNode()
        {
            SkillTreeDefinition tree = CreateRadiationLikeTree();

            Assert.AreEqual(0, tree.GetAbilities(0).Count);
            Assert.AreSame(_crackle, tree.GetAbilities(1)[0]);

            List<AbilityDefinition> afterThree = tree.GetAbilities(3);
            Assert.AreEqual(2, afterThree.Count);
            Assert.IsTrue(afterThree.Contains(_crackleII));
            Assert.IsTrue(afterThree.Contains(_lightningRod));
            Assert.IsFalse(afterThree.Contains(_crackle));
        }

        [Test]
        public void GetAbilities_CountAboveNodeCount_GivesTheWholeTree()
        {
            SkillTreeDefinition tree = CreateRadiationLikeTree();

            Assert.AreEqual(tree.GetAbilities(3).Count, tree.GetAbilities(99).Count);
        }

        [Test]
        public void ValidTree_PassesValidation()
        {
            Assert.IsTrue(CreateRadiationLikeTree().TryValidate(out string problem), problem);
        }

        [Test]
        public void UpgradeOfAnAbilityNotYetGiven_FailsValidation()
        {
            SkillTreeDefinition tree = CreateTree(TestSkillTrees.Upgrade(_crackleII, _crackle));

            Assert.IsFalse(tree.TryValidate(out _));
        }

        [Test]
        public void NodeWithNoAbilities_FailsValidation()
        {
            SkillTreeDefinition tree = CreateTree(
                Node(_crackle),
                new SkillNode(new List<AbilityDefinition>(), null));

            Assert.IsFalse(tree.TryValidate(out _));
        }

        [Test]
        public void TooManyOrTooFewNodes_FailValidation()
        {
            Assert.IsFalse(CreateTree().TryValidate(out _));

            SkillTreeDefinition sixNodes = CreateTree(
                Node(_crackle), Node(_crackle), Node(_crackle), Node(_crackle), Node(_crackle), Node(_crackle));
            Assert.IsFalse(sixNodes.TryValidate(out _));
        }

        [Test]
        public void EquipmentTree_IsOnlyUsableWithItsTagWorn()
        {
            SkillTreeDefinition tree = CreateTree(Node(_crackle));
            SerializedObject serializedTree = new SerializedObject(tree);
            serializedTree.FindProperty("_requiresEquipment").boolValue = true;
            serializedTree.FindProperty("_requiredTag").enumValueIndex = (int)EquipmentTag.Bladed;
            serializedTree.ApplyModifiedPropertiesWithoutUndo();

            Loadout loadout = new Loadout(new CharacterStats(Archetype.Martial));
            Assert.IsFalse(tree.IsUsableWith(loadout));

            WeaponDefinition sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/_Project/Data/Items/Weapon_Sword.asset");
            Assert.IsNotNull(sword, "Missing Weapon_Sword.asset");
            Assert.IsTrue(loadout.TryEquip(sword, out string reason), reason);
            Assert.IsTrue(tree.IsUsableWith(loadout));
        }

        [Test]
        public void TreeWithoutEquipment_IsAlwaysUsable()
        {
            SkillTreeDefinition tree = CreateRadiationLikeTree();
            Loadout emptyLoadout = new Loadout(new CharacterStats(Archetype.Magic));

            Assert.IsTrue(tree.IsUsableWith(emptyLoadout));
        }

        // Crackle -> Crackle II (replaces Crackle) -> Lightning Rod. Names borrowed from Radiation; not the real data.
        private SkillTreeDefinition CreateRadiationLikeTree()
        {
            return CreateTree(
                Node(_crackle),
                TestSkillTrees.Upgrade(_crackleII, _crackle),
                Node(_lightningRod));
        }

        private static SkillNode Node(AbilityDefinition ability)
        {
            return TestSkillTrees.Node(ability);
        }

        private static SkillTreeDefinition CreateTree(params SkillNode[] nodes)
        {
            return TestSkillTrees.Create(Archetype.Magic, nodes);
        }
    }
}
