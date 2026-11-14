using System.Collections.Generic;
using System.Reflection;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Builds skill trees and abilities in memory for tests, so no assets are needed.</summary>
    public static class TestSkillTrees
    {
        /// <summary>
        /// Makes a tree with the given archetype and nodes. Those fields are private (designers fill them in the
        /// Inspector), so this sets them through reflection: looking each field up by name and writing it directly.
        /// </summary>
        public static SkillTreeDefinition Create(Archetype archetype, params SkillNode[] nodes)
        {
            SkillTreeDefinition tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            SetField(tree, "_archetype", archetype);
            SetField(tree, "_nodes", new List<SkillNode>(nodes));
            return tree;
        }

        /// <summary>A normal node granting one ability.</summary>
        public static SkillNode Node(AbilityDefinition ability)
        {
            return new SkillNode(new List<AbilityDefinition> { ability }, null);
        }

        /// <summary>An upgrade node: grants newAbility and replaces oldAbility.</summary>
        public static SkillNode Upgrade(AbilityDefinition newAbility, AbilityDefinition oldAbility)
        {
            return new SkillNode(new List<AbilityDefinition> { newAbility }, oldAbility);
        }

        /// <summary>A placeholder ability with a readable name.</summary>
        public static AbilityDefinition Ability(string abilityName)
        {
            TestAbility ability = ScriptableObject.CreateInstance<TestAbility>();
            ability.name = abilityName;
            return ability;
        }

        private static void SetField(SkillTreeDefinition tree, string fieldName, object value)
        {
            FieldInfo field = typeof(SkillTreeDefinition).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(tree, value);
        }
    }
}
