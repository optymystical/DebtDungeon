# Coding Standards & Best Practices

**Project:** Debt Dungeon · **Team:** Debt Collectors (Christopher Caplinger, Matthew Phillips, Larry Miller, Gregory Rooney)
**Engine:** Unity 6.3 LTS (6000.3.27f2), C# · **Repository:** https://github.com/optymystical/DebtDungeon

## Purpose

These standards establish a consistent approach to writing and organizing code for the project.

These standards apply to all code within the project including code authored by the development team directly as well as using generative AI to create or modify code.

**Scope:** these standards apply to everything in the main project repository. Module Week prototypes (separate Unity projects that are not imported into the main project) may follow them loosely, but any code rewritten into the main project must meet them.

**Consistency over preference:** where a rule below differs from how someone normally writes code, the team rule wins. Changes to this document are agreed on at the weekly team meeting.

---

# 1. General Principles

- **Readability:** another developer should be able to understand any code in the project without the author explaining it.
- **Consistency:** similar problems are solved in similar ways throughout the project.
- **Maintainability:** changes and additions can be made without touching unrelated systems.
- **Simplicity:** prefer the simplest solution that solves the problem. No frameworks, patterns or optimizations the project doesn't need yet.
- **Separation of responsibilities:** each class and each system has one clear purpose.
- **Minimal coupling:** modules talk to each other only through the shared Core layer (interfaces, data types and events), never by reaching into another module's classes.
- **Data over code:** design values (stats, abilities, units, chunks, loot) live in ScriptableObjects so they can be tuned without code changes.
- **Performance awareness:** avoid expensive work in code that runs every frame.
- **Team compatibility:** code must work cleanly with the team's Git workflow (Section 8).

---

# 2. Project Organization

All project content lives under `Assets/_Project/`. Code is organized **by module**, and each module folder has its own Assembly Definition. Third-party content goes in `Assets/ThirdParty/` and is never edited directly.

```text
Assets/
└── _Project/
    ├── Core/          Shared interfaces, data types and events (DebtDungeon.Core). Changes need team sign-off.
    ├── FloorGen/      Chunk and heat-map floor generation
    ├── AI/            Faction AI, spawners, raids, pathfinding
    ├── Player/        Player controller, combat, abilities, HUD
    ├── Progression/   Character creator, stats, equipment, XP, skill trees
    ├── Inventory/     Items, inventory, loot
    ├── Game/          The only assembly that references other modules; wires them together
    ├── Editor/        Editor-only tools
    ├── Art/           Tiles, Characters, Enemies, Props, UI, FX
    ├── Audio/         Music, SFX
    ├── Data/          ScriptableObject assets: Chunks, Units, Factions, Abilities, Presets, SkillTrees, Items
    ├── Prefabs/       Grouped into subfolders by module (Prefabs/AI, Prefabs/Player, ...)
    ├── Scenes/        Main, MainMenu, and Sandbox/ (personal test scenes: Sandbox/<YourName>_<Topic>)
    └── Settings/      URP, Input Actions and other settings assets
```

**Assembly rules**

| Assembly | May reference |
| --- | --- |
| `DebtDungeon.Core` | Nothing (only Unity) |
| `DebtDungeon.FloorGen`, `.AI`, `.Player`, `.Progression`, `.Inventory` | `DebtDungeon.Core` and the Unity packages they need |
| `DebtDungeon.Game` | Core and every module |

The compiler enforces these rules, so a module that tries to use another module's class won't build.

---

# 3. Naming Conventions

Comments and developer-created names use U.S. English spelling and grammar. Use clear, descriptive names; avoid abbreviations except widely known ones (`UI`, `HP`, `XP`, `AI`, `Id`).

### Classes and Methods

Use **PascalCase** for classes, structs, enums, enum values, methods, properties, events and namespaces.

```csharp
namespace DebtDungeon.Player
{
    public class PlayerController : MonoBehaviour
    {
        public void TakeDamage(DamageInfo damage) { }
    }
}
```

| Kind | Convention | Example |
| --- | --- | --- |
| Namespace | `DebtDungeon.<Module>` | `DebtDungeon.AI` |
| Interface | `I` + PascalCase | `IDamageable`, `IFloorQuery` |
| ScriptableObject data class | PascalCase + `Definition` or `Config` | `AbilityDefinition`, `UnitConfig` |
| Enum and values | PascalCase, singular name | `enum Faction { Neutral, Player, Goblin, Spider }` |
| Event | PascalCase, past tense | `FloorGenerated`, `ActorKilled` |
| Event handler method | `On` + event name | `OnActorKilled` |
| Method | PascalCase verb phrase | `SpawnRaidParty()`, `RecalculateStats()` |

### Variables

| Kind | Convention | Example |
| --- | --- | --- |
| Private and protected fields | `_camelCase` | `private int _currentHealth;` |
| Serialized private fields | `_camelCase` | `[SerializeField] private float _moveSpeed = 5f;` |
| Local variables and parameters | `camelCase` | `float horizontalInput` |
| Constants | PascalCase | `private const int MaxBurdenSteps = 2;` |
| Properties | PascalCase | `public int CurrentHealth => _currentHealth;` |

The underscore marks a field at a glance, so a method's locals and the object's state are never confused.

**Booleans** use names that read as a true/false question:

```csharp
private bool _isDead;
private bool _canAttack;
private bool _hasExitKey;
```

Avoid vague names such as `value`, `thing`, `state`, `flag`, `temp` or `data2`.

### Script Files

- The filename of a C# script matches the name of its primary class: `PlayerController.cs` contains `public class PlayerController`.
- One primary class per file. Small helper types used only by that class (a private struct or enum) may share its file.

### Asset Names

| Asset | Convention | Example |
| --- | --- | --- |
| Sprites and sprite sheets | `spr_<category>_<name>_<variant>` | `spr_enemy_goblin_walk` |
| Prefabs | PascalCase | `GoblinGrunt`, `HealthPotion` |
| ScriptableObject assets | `<Type>_<Name>` | `Ability_Crackle`, `Unit_Skitterling`, `Chunk_GoblinMajor_01` |
| Scenes | PascalCase | `Main`, `MainMenu`, `Sandbox/Chris_SkillTrees` |
| Audio | `sfx_<name>` / `mus_<name>` | `sfx_goblin_warhorn`, `mus_dungeon_loop` |

---

# 4. Code Organization and Formatting

### Class purpose

Each class has one clear purpose:

```text
PlayerController   → reads input and moves the player
Health             → tracks hit points, takes damage, raises death events
RaidManager        → forms and sends a faction's raid parties
SkillTreeUI        → displays a skill tree and forwards clicks to the progression system
```

Avoid one large class that handles unrelated systems. If a class's description needs the word "and" more than once, split it.

### Indentation and spacing

- **4 spaces** per indent level. No tabs. (Set automatically by the repository's `.editorconfig`.)
- **Allman braces:** opening brace on its own line, for classes, methods and control blocks.
- **Always use braces**, even for one-line `if`, `for` and `while` bodies.
- One statement per line; one variable declaration per line.
- One blank line between methods and between groups of fields. No more than one blank line in a row.
- Spaces around operators (`a + b`, `x = 5`) and after commas and keywords (`if (isDead)`).
- Keep lines under about 120 characters. Break long parameter lists one per line.

```csharp
if (_isDead)
{
    return;
}

for (int i = 0; i < _spawnPoints.Count; i++)
{
    SpawnUnit(_spawnPoints[i]);
}
```

### Member order inside a class

1. Constants
2. Serialized fields (grouped with `[Header]`)
3. Private fields
4. Properties
5. Events
6. Unity messages, in lifecycle order: `Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `OnDisable`, `OnDestroy`
7. Public methods
8. Private methods

---

# 5. Inspector Variables

Use `[SerializeField] private` for values that need to be edited in the Inspector but not changed by other scripts. **No public fields.** If other scripts need to read a value, expose a read-only property.

```csharp
[Header("Health")]
[Tooltip("Hit points at full health before stat scaling.")]
[SerializeField, Min(1)] private int _baseMaxHealth = 100;
[SerializeField, Range(0f, 2f)] private float _invulnerabilityDuration = 0.5f;

public int BaseMaxHealth => _baseMaxHealth;
```

- Group related fields with `[Header("...")]`.
- Add `[Tooltip("...")]` to any value a designer is expected to tune.
- Use `[Min]` or `[Range]` to stop invalid values (negative speeds, zero cooldowns).
- Assign references (other components, prefabs, ScriptableObjects, `LayerMask`s) in the Inspector instead of looking them up by name in code.
- Use `[RequireComponent(typeof(...))]` when a script depends on another component on the same GameObject.

---

# 6. Methods

Methods have a clear, focused purpose and a verb-phrase name.

Good:

```csharp
TakeDamage();
UpdateHealthBar();
SpawnRaidParty();
TryUnlockNode();
```

Avoid vague names: `DoThing()`, `Process()`, `HandleStuff()`, `Run2()`.

- **Size:** if a method grows past about 30–40 lines or needs comments to separate its "steps," split the steps into private methods.
- **Parameters:** more than 4 parameters usually means they belong together in a struct (for example, `DamageInfo` instead of five separate damage values).
- **Guard clauses:** check invalid cases early and return, instead of nesting the whole method inside `if` blocks.
- **Try pattern:** methods that can fail without it being an error return `bool` and are named `Try...` (for example, `TrySpendSkillPoint(...)`).
- **Explicit access modifiers** on every member (`private void Update()`, not `void Update()`).

```csharp
public bool TryUnlockNode(SkillNodeDefinition node)
{
    if (_availableSkillPoints <= 0)
    {
        return false;
    }

    if (!IsPreviousNodeUnlocked(node))
    {
        return false;
    }

    _availableSkillPoints--;
    GrantNodeAbility(node);
    return true;
}
```

---

# 7. Commenting Conventions

**Write self-documenting code first.** Clear names remove the need for most comments.

Bad:

```csharp
t = d * (cb - cdb);
```

Good:

```csharp
totalDamage = baseDamage * (currentBuffs - currentDebuffs);
```

**Use comments to explain *why*, not *what*.**

Bad:

```csharp
// Start the spawn delay.
yield return new WaitForSeconds(_spawnDelay);
```

Good:

```csharp
// Delay spawning until the door animation finishes.
yield return new WaitForSeconds(_spawnDelay);
```

Avoid comments that repeat the code:

```csharp
// Add one to score.   <-- remove
score++;
```

**XML documentation comments** are required on every public type and public member in `Core`, and on public methods other modules or teammates are expected to call:

```csharp
/// <summary>
/// Applies damage after armor and Shroud absorption. Raises ActorKilled if health reaches zero.
/// </summary>
public void TakeDamage(DamageInfo damage)
```

**TODO comments** mark known unfinished work and name who owns it:

```csharp
// TODO(Chris): Replace placeholder ability icons with final art.
```

- Don't commit commented-out code. Git history keeps old versions.
- Formulas taken from Flow State or the GDD get a short comment saying where they come from (for example, `// Power = stat / 10 (GDD: Stats)`).

---

# 8. General Style Issues

### Data types

- `float` for Unity math (positions, speeds, timers). Don't use `double`; Unity's APIs use `float`.
- `int` for counts, hit points, stat values and points.
- `Vector2` for world positions in 2D, and `Vector2Int` for tile and chunk grid cells.
- **Enums instead of strings or magic numbers** for fixed sets: factions, damage types, stats, archetypes, states.
- `List<T>` for collections that change size, arrays for fixed-size data, `Dictionary<TKey, TValue>` for lookups by key.
- `var` only when the type is obvious from the right-hand side (`var spawnPoints = new List<SpawnPoint>();`). Otherwise write the type.

### Magic numbers

No unexplained numbers in logic. Use a serialized field, a ScriptableObject value or a named constant.

```csharp
// Bad
speed = baseSpeed * (1f - 0.08f * steps);

// Good
private const float MovePenaltyPerBurdenStep = 0.08f;
speed = _baseSpeed * (1f - MovePenaltyPerBurdenStep * _burdenSteps);
```

### Null handling

- Check references from the Inspector in `Awake()` and log a clear error if one is missing.
- Don't use `?.` or `??` on Unity objects (`GameObject`, `Component`, `ScriptableObject`). Unity overrides `==` for destroyed objects, and those operators skip that check. Use `if (target != null)`.

### Errors and logging

- Use `Debug.LogError(message, this)` or `Debug.LogWarning(message, this)`. Passing `this` lets you click the log to find the object.
- Don't leave per-frame `Debug.Log` calls in committed code.
- Don't catch exceptions just to hide them.

### Libraries and packages

The project uses only these packages: Input System, 2D Sprite, 2D Tilemap Editor, 2D Tilemap Extras, 2D Animation, 2D Aseprite Importer, Universal RP, Unity UI (uGUI + TextMeshPro), Cinemachine and Test Framework.

- **Adding a package or Asset Store asset needs team agreement** at the weekly meeting.
- Use the **new Input System** through the project's Input Actions asset. Don't use the old `Input.GetAxis` / `Input.GetKey` API.
- UI uses uGUI with TextMeshPro. Don't use the legacy `Text` component.

### Complexity

- Use only the design patterns the project already uses: interfaces through Core, C# events (`GameEvents`), ScriptableObjects for data and simple state machines for AI and game flow.
- Static classes only for stateless helpers and the shared `GameEvents` hub. Avoid other singletons and static mutable state.
- Don't add pooling or other optimization systems unless the Profiler shows a real problem.
- **Keep changes focused:** when working on a task, don't modify unrelated code.
- **Reuse existing systems:** before writing a new system, check whether one already exists, and extend it instead of building a competing one.

### Workflow and version control

See `Docs/GitWorkflow.md` for the day-to-day routine.

- Branch from `develop` as `feature/<name>-<topic>` (e.g. `feature/chris-skill-trees`), and merge back through a pull request. Never push to `main`.
- Every pull request is reviewed by at least one other teammate before merging.
- The code must compile with no errors and no new warnings, and must be tested in Unity before it's committed.
- Commit messages are short imperative sentences: `Add raid timer to RaidManager`, `Fix burden cap check`.
- Only one person edits a given scene at a time; say so in Discord first. Put work in prefabs where possible.
- Never commit `Library/`, `Temp/`, `Logs/` or `UserSettings/` (handled by `.gitignore`).

---

# 9. Unity Practices

Use Unity's lifecycle methods for their intended purpose:

- `Awake()`: initialize the object itself and get references to its **own** components.
- `OnEnable()` / `OnDisable()`: subscribe to and unsubscribe from events. **Every subscription must have a matching unsubscription.**
- `Start()`: initialization that depends on **other** objects being set up.
- `Update()`: frame-based gameplay, timers and input.
- `FixedUpdate()`: physics. All `Rigidbody2D` movement happens here.

Avoid searches and expensive operations every frame. Get a reference once and reuse it.

```csharp
// Bad: in Update()
GameObject.Find("Player").GetComponent<Health>();

// Good
[SerializeField] private Health _playerHealth;   // assigned in the Inspector
```

- **Never use** `GameObject.Find`, `FindObjectOfType` / `FindFirstObjectByType` or `GetComponent` inside `Update` or `FixedUpdate`.
- Use `CompareTag("Player")` instead of `tag == "Player"`.
- Use `Time.deltaTime` for frame-based movement and timers, and `Time.fixedDeltaTime` inside `FixedUpdate`.
- Use `[SerializeField] private LayerMask` fields for raycasts and overlap checks instead of hard-coded layer numbers.
- **ScriptableObjects hold design data only.** Never change a ScriptableObject's values at runtime, because the changes stay in the asset in the Editor. Copy the values into a runtime object (e.g. a `CharacterStats` instance) and change that.
- Coroutines are fine for timed sequences (wind-ups, respawn delays). Stop them in `OnDisable()` if the object can be disabled mid-sequence.
- Prefer prefabs and prefab variants over editing objects directly in a scene.
- Prefer events over polling: listen for `ActorKilled` instead of checking every frame whether an enemy has died.

---

# 10. Generative AI Policies

Generative AI may be used to assist with development, as permitted by the course.

AI-generated code must:

- Follow the standards in this document.
- Follow the project's existing architecture: module folders, Core contracts and assembly rules.
- Reuse existing systems rather than creating duplicates.
- Avoid introducing new packages or frameworks.
- Be reviewed, understood and tested in Unity by the developer before it is committed.
- Arrive with **Defense Notes**: a short explanation of how it works, its key data structures and variables, why the approach was chosen, and how it connects to other modules. The developer keeps these notes (in the pull request description or their own notes) for code review and whiteboard defense.

Additional rules for this team:

- **AI tools never push to the repository.** All commits are made by a team member who has reviewed the code.
- Pull requests that contain AI-assisted code say so in the description, so the reviewer knows to check the Defense Notes.
- AI-generated art and placeholder assets follow the same naming and folder rules as hand-made assets.

The developer is responsible for understanding and testing any AI-generated code included in the project.

### Whiteboard Defense

All code must be able to pass a "whiteboard defense." Developers using generative AI must be able to explain and reason about the systems they create or incorporate, including:

- How the system works.
- What data structures or important variables are being used and why.
- Why the chosen approach is appropriate for the problem.
- How the system interacts with other parts of the project.

Developers are not expected to memorize AI-generated code. They are expected to understand the code well enough to explain it, modify it, troubleshoot it, and answer questions about its behavior.

---

## Team agreement

By committing code to this repository, each team member agrees to follow these standards.

| Team member | Agreed |
| --- | --- |
| Christopher Caplinger | ☐ |
| Matthew Phillips | ☐ |
| Larry Miller | ☐ |
| Gregory Rooney | ☐ |
