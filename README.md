# Debt Dungeon

A real-time dungeon-crawler RPG by **Debt Collectors**, Full Sail University midterm project.

- **Engine:** Unity **6.3 LTS — 6000.3.27f2**
- **Template:** Universal 2D (URP)
- **Platform:** Windows PC, keyboard + mouse

## First-time setup (every team member)

1. Install **Git** and **Git LFS**, then run once: `git lfs install`
2. Install Unity **6000.3.27f2** through Unity Hub (modules: Visual Studio Community, Documentation).
3. Clone the repo: `git clone <repo URL>`
4. Unity Hub → **Add** → select the cloned folder → open it with 6000.3.27f2.
5. The first open takes a few minutes while Unity rebuilds the `Library/` folder. That folder is ignored by Git on purpose.

### Optional: Unity Smart Merge for scene/prefab conflicts
Add this to your global Git config (`git config --global --edit`), using your own Unity path:

```
[merge]
    tool = unityyamlmerge
[mergetool "unityyamlmerge"]
    trustExitCode = false
    cmd = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f2/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
```
If a merge conflicts on a `.unity` or `.prefab` file, run `git mergetool`.

## Branches

| Branch | Purpose |
| --- | --- |
| `main` | Milestone-ready builds only. Protected; merges come from `develop` only. |
| `develop` | Integration branch. Everything merges here through pull requests. |
| `feature/<name>-<short-topic>` | Your work, e.g. `feature/chris-skill-trees`. Branch from `develop`, PR back into `develop`. |

| `feature/greg-floor-generation` | Gregory: Floor & Levels (Module 1) |
| `feature/larry-faction-ai` | Larry: Faction AI (Module 2) |
| `feature/matthew-player-combat` | Matthew: Player & Combat (Module 3) |
| `feature/chris-progression`, `feature/chris-core` | Christopher: Progression (Module 4), Core contracts |
| `prototype/greg-floor-generation` | Greg's Module Week prototype. Reference only, **never merged**. |

**Rules:** pull `develop` before starting work · small, frequent PRs · one person edits a given scene at a time (say so in Discord) · work inside prefabs where possible.

The full day-to-day routine is in **[Docs/GitWorkflow.md](Docs/GitWorkflow.md)**.

## Docs

| File | What's in it |
| --- | --- |
| [Docs/AlphaScope.md](Docs/AlphaScope.md) | What's in the Alpha, who owns what, design rules |
| [Docs/CodingStandards.md](Docs/CodingStandards.md) | Team coding standards (everyone agrees to these) |
| [Docs/GitWorkflow.md](Docs/GitWorkflow.md) | Branches, daily routine, PRs, scene rules, conflicts |
| `Assets/_Project/<Module>/README.md` | Per-module notes: owner, scope, Core contracts, folders |
| `Assets/_Project/Core/CHANGELOG.md` | Core versions and pending changes |

## Scenes

| Scene | In build | Purpose |
| --- | --- | --- |
| `Scenes/MainMenu` | 0 | Title and character creation |
| `Scenes/Main` | 1 | The dungeon run |
| `Scenes/Sandbox/Greg_FloorGen` | no | Gregory's test scene |
| `Scenes/Sandbox/Larry_FactionAI` | no | Larry's test scene |
| `Scenes/Sandbox/Matthew_PlayerCombat` | no | Matthew's test scene |
| `Scenes/Sandbox/Chris_Progression` | no | Christopher's test scene |

Your sandbox scene is yours alone. `Main` and `MainMenu` are shared: ask in Discord before editing.

## Folder structure

```
Assets/
└── _Project/
    ├── Core/         shared interfaces & events (DebtDungeon.Core). Changes need team sign-off.
    ├── FloorGen/     chunk + heat-map floor generation
    ├── AI/           faction AI, spawners, pathfinding
    ├── Player/       player controller, combat, abilities, HUD
    ├── Progression/  character creator, stats, XP, skill trees
    ├── Inventory/    items, equipment, loot
    ├── Game/         the only assembly that references every module; wires them together
    ├── Editor/       editor-only tools
    ├── Art/          Tiles, Characters, Enemies, Props, UI, FX
    ├── Audio/        Music, SFX
    ├── Data/         ScriptableObject assets: Chunks, Units, Factions, Abilities, Presets, SkillTrees, Items
    ├── Prefabs/      one subfolder per module (FloorGen/Chunks, AI, Player, Progression, Game)
    ├── Scenes/       Main, MainMenu, Sandbox/
    └── Settings/     URP, Input Actions, and other settings assets
```

Each module folder has its own Assembly Definition and references **only** `DebtDungeon.Core`.
To recreate the layout, use the menu **Debt Dungeon → Create Project Structure**.

## Packages

| Package | Why |
| --- | --- |
| Input System | Player controls (the new Input System) |
| 2D Sprite, 2D Tilemap Editor, 2D Tilemap Extras | Pixel art and chunk Tilemaps |
| 2D Animation | Sprite animation |
| 2D Aseprite Importer | Import .aseprite pixel art directly |
| Universal RP | Rendering (included with the Universal 2D template) |
| Unity UI (uGUI + TextMeshPro) | Menus, HUD, creator, skill tree UI |
| Cinemachine | Camera follow |
| Test Framework | Optional unit tests |

## Layers (Project Settings → Tags and Layers)

- **Sorting layers:** `Floor`, `Walls`, `Props`, `Actors`, `FX`, `UI`
- **Physics layers:** `Player`, `Goblin`, `Spider`, `Walls`, `Projectile`

Ask Christopher before changing Project Settings; they're shared by everyone.

## Art import settings
16 Pixels Per Unit · Filter Mode **Point (no filter)** · Compression **None**. 1 tile = 1 world unit.
