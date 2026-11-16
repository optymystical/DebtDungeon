using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>Checks the 4 Alpha skill tree assets (placeholder abilities) are set up correctly.</summary>
    public class SkillTreeAssetTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Progression/ProgressionCatalog.asset";

        [TestCase(Archetype.Martial)]
        [TestCase(Archetype.Magic)]
        public void EachAlphaArchetype_HasTwoValidTreesOfThreeNodes(Archetype archetype)
        {
            ProgressionCatalog catalog = AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, $"Missing {CatalogPath}");

            List<SkillTreeDefinition> trees = catalog.GetSkillTrees(archetype);

            Assert.AreEqual(2, trees.Count);
            foreach (SkillTreeDefinition tree in trees)
            {
                Assert.IsTrue(tree.TryValidate(out string problem), $"{tree.name}: {problem}");
                Assert.AreEqual(3, tree.NodeCount, tree.name);
            }
        }

        [Test]
        public void BladedTree_NeedsABladedWeapon_RadiationNeedsNothing()
        {
            ProgressionCatalog catalog = AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(CatalogPath);

            foreach (SkillTreeDefinition tree in catalog.GetSkillTrees(Archetype.Martial))
            {
                Assert.IsTrue(tree.RequiresEquipment, tree.name);
            }

            foreach (SkillTreeDefinition tree in catalog.GetSkillTrees(Archetype.Magic))
            {
                Assert.IsFalse(tree.RequiresEquipment, tree.name);
            }
        }
    }
}
