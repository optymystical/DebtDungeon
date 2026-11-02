// Assets/_Project/Editor/ProjectStructureSetup.cs
// Editor-only tool: adds the menu item "Debt Dungeon > Create Project Structure".
// Run it once in the main project. It is safe to run again: it never overwrites existing files.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace DebtDungeon.EditorTools
{
    /// <summary>
    /// Creates the Debt Dungeon folder layout under Assets/_Project and one
    /// Assembly Definition (.asmdef) per module, wired so modules reference only Core.
    /// </summary>
    public static class ProjectStructureSetup
    {
        private const string Root = "Assets/_Project";

        /// <summary>Folders created under Assets/_Project (module folders are added from Modules below).</summary>
        private static readonly string[] Folders =
        {
            "Art/Tiles", "Art/Characters", "Art/Enemies", "Art/Props", "Art/UI", "Art/FX",
            "Audio/Music", "Audio/SFX",
            "Data/Chunks", "Data/Units", "Data/Abilities", "Data/Presets", "Data/SkillTrees", "Data/Items",
            "Prefabs", "Scenes", "Settings"
        };

        /// <summary>One entry per code module: its folder, assembly name and the assemblies it may reference.</summary>
        private struct ModuleAssembly
        {
            public string Folder;
            public string AssemblyName;
            public string[] References;

            public ModuleAssembly(string folder, string assemblyName, params string[] references)
            {
                Folder = folder;
                AssemblyName = assemblyName;
                References = references;
            }
        }

        // Core references nothing. Every gameplay module references only Core (plus Unity packages it needs).
        // Game is the one "glue" assembly allowed to see every module; it wires real systems together in the main scene.
        private static readonly ModuleAssembly[] Modules =
        {
            new ModuleAssembly("Core",        "DebtDungeon.Core"),
            new ModuleAssembly("FloorGen",    "DebtDungeon.FloorGen",    "DebtDungeon.Core"),
            new ModuleAssembly("AI",          "DebtDungeon.AI",          "DebtDungeon.Core"),
            new ModuleAssembly("Player",      "DebtDungeon.Player",      "DebtDungeon.Core", "Unity.InputSystem", "UnityEngine.UI", "Unity.TextMeshPro"),
            new ModuleAssembly("Progression", "DebtDungeon.Progression", "DebtDungeon.Core", "UnityEngine.UI", "Unity.TextMeshPro"),
            new ModuleAssembly("Inventory",   "DebtDungeon.Inventory",   "DebtDungeon.Core", "UnityEngine.UI", "Unity.TextMeshPro"),
            new ModuleAssembly("Game",        "DebtDungeon.Game",
                "DebtDungeon.Core", "DebtDungeon.FloorGen", "DebtDungeon.AI", "DebtDungeon.Player",
                "DebtDungeon.Progression", "DebtDungeon.Inventory", "Unity.InputSystem")
        };

        [MenuItem("Debt Dungeon/Create Project Structure")]
        public static void CreateStructure()
        {
            int created = 0;

            foreach (string folder in Folders)
            {
                created += CreateFolderWithKeep(Root + "/" + folder);
            }

            foreach (ModuleAssembly module in Modules)
            {
                string folderPath = Root + "/" + module.Folder;
                created += CreateFolderWithKeep(folderPath);
                created += CreateAsmdef(folderPath, module);
            }

            AssetDatabase.Refresh(); // make Unity notice the new files and import them
            Debug.Log($"[Debt Dungeon] Project structure ready ({created} new items created).");
        }

        /// <summary>
        /// Creates a folder (if missing) plus an empty .gitkeep file, because Git does not track empty folders.
        /// Unity skips files whose names start with '.', so .gitkeep never gets a .meta file.
        /// Returns 1 if the folder was new, 0 if it already existed.
        /// </summary>
        private static int CreateFolderWithKeep(string path)
        {
            bool isNew = !Directory.Exists(path);
            Directory.CreateDirectory(path); // does nothing if the folder already exists

            string keepFile = Path.Combine(path, ".gitkeep");
            if (!File.Exists(keepFile))
            {
                File.WriteAllText(keepFile, string.Empty);
            }

            return isNew ? 1 : 0;
        }

        /// <summary>
        /// Writes "<AssemblyName>.asmdef" into the module folder unless one already exists.
        /// References are by assembly name, so Unity resolves them once all the asmdefs are imported.
        /// Returns 1 if a file was written, 0 if it was skipped.
        /// </summary>
        private static int CreateAsmdef(string folderPath, ModuleAssembly module)
        {
            string asmdefPath = Path.Combine(folderPath, module.AssemblyName + ".asmdef");
            if (File.Exists(asmdefPath))
            {
                return 0; // never overwrite a teammate's edited asmdef
            }

            // Build the JSON by hand to keep this tool dependency-free and easy to read.
            string references = module.References.Length == 0
                ? string.Empty
                : "\"" + string.Join("\", \"", module.References) + "\"";

            string json =
                "{\n" +
                $"    \"name\": \"{module.AssemblyName}\",\n" +
                $"    \"rootNamespace\": \"{module.AssemblyName}\",\n" +
                $"    \"references\": [{references}],\n" +
                "    \"includePlatforms\": [],\n" +
                "    \"excludePlatforms\": [],\n" +
                "    \"allowUnsafeCode\": false,\n" +
                "    \"autoReferenced\": true,\n" +
                "    \"noEngineReferences\": false\n" +
                "}\n";

            File.WriteAllText(asmdefPath, json);
            return 1;
        }
    }
}
