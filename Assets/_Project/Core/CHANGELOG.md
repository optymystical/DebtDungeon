# Core changelog

Every Core change gets a version bump and an entry here. "Affects" lists module numbers:
1 FloorGen, 2 AI, 3 Player & Combat, 4 Progression, UI = HUD/menus.

## v1.2 — Alpha sprint (Nov 10, 2026)

First version committed to the repo. Everything below is new compared with the v1.1 plan.

**Enums**
- `Faction` (Neutral, Player, Goblin, Spider) lives only in Core; FloorGen's own copy goes away. *(1, 2)*
- `StatType` is the 9 Flow State stats, grouped Martial → Magic → Mental. `Archetype` is Martial, Magic, Mental
  (Mental is Beta but stays so the stat math holds). *(3, 4)*
- New `SpawnPointType` (Spawner, SingleEnemy, Chest, Station), `HitResult` (Miss, Hit, Critical),
  `AbilityKind` (BasicAttack, Defensive, Active, Passive). *(1, 2, 3)*

**Stats** *(3, 4)*
- `StatRules`: `GetArchetype`, `BelongsTo` (stat ÷ 3), `GetDefensiveStat`, `GetPower` (stat ÷ 10).
- `StatModifier` is a read-only struct (Stat, Value, Source).
- `IStatProvider.RemoveModifier(mod)` is replaced by `RemoveModifiersFromSource(source)`; adds `Archetype`.

**Combat** *(2, 3, 4)*
- `CombatRules.RollHit(accuracy)` and `GetDamage(baseDamage, result)`. Hit chance = min(Accuracy, 100%);
  Accuracy above 100% is crit chance, capped at 100%; crits deal 2×.
- `DamageInfo` is a read-only struct: Amount, Type, Source, SourceFaction, IsCritical.
- New `IDamageAbsorber` for armor and Shrouds (Durability + Limit), applied by `Health`.
- New `ICombatState { bool InCombat }`. Player implements it; the station and upgrade UI read it.
- New `IKillReward` (XpReward, GoldReward, DisplayName) on enemies and spawners.
- New `KillInfo` struct: victim, killer, both factions, killer name, position, XP and gold.

**Health** *(2, 3, 4)*
- Shared `Health` component in Core: faction, max HP, optional i-frames, absorbers, `HealthChanged`, `Damaged`, `Died`.
- Ignores hits from its own faction (traps are Neutral, so they hurt everyone).
- Raises `GameEvents.ActorKilled` on every death and `PlayerDied` for the player. It never destroys its GameObject.

**Abilities** *(3, 4)*
- `AbilityDefinition` gains `Kind`; fields are `[SerializeField] private` with read-only properties.
- `IAbilityUser` gains `RevokeAbility` and `HasAbility`. Progression only grants abilities whose equipment is worn,
  so the runner never checks gear.

**Floor** *(1, 2, 3)*
- `SpawnPoint` gains `Type`. `IFloorQuery` gains `GetMoveSpeedMultiplier(tileCell)` (water) and `TileToChunkCell`.
  Parameter names now say whether they take a tile cell or a chunk cell.

**Events** *(all)*
- `ActorKilled` and `PlayerDied` now send `KillInfo`. New `PlayerLeveledUp(int)`, `SpawnerDestroyed(Faction, Vector3)`,
  `GoldCollected(int)`. All listeners are cleared when Play mode starts.

## v1.1 — contract from the Alpha plan (never committed)

Faction and ChunkIntensity enums; `DamageType`, `DamageInfo`, `IDamageable`; `Archetype`, placeholder `StatType`,
`StatRules`, `StatModifier`, `IStatProvider`; `AbilityDefinition`, `IAbilityUser`; `SpawnPoint`, `IFloorQuery`;
`GameEvents` (`FloorGenerated`, `ActorKilled`, `PlayerDied`).
