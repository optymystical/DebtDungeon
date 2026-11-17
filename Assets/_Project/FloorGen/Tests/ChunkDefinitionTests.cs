using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace DebtDungeon.FloorGen.Tests
{
    /// <summary>Edit Mode tests for chunk authoring (Trello: "Chunk definition and authoring workflow").</summary>
    public class ChunkDefinitionTests
    {
        // Every chunk asset in the project must follow the chunk rules, so a broken chunk can't reach a floor
        [Test]
        public void EveryChunkDefinition_PassesValidation()
        {
            string[] guids = AssetDatabase.FindAssets("t:ChunkDefinition");
            Assert.Greater(guids.Length, 0, "No ChunkDefinition assets found.");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ChunkDefinition chunk = AssetDatabase.LoadAssetAtPath<ChunkDefinition>(path);
                List<string> problems = chunk.Validate();

                Assert.IsEmpty(problems, path + ": " + string.Join(" | ", problems));
            }
        }
    }
}
