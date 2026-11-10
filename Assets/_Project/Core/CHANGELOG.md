# Core changelog

Every Core change gets a version bump and an entry here. "Affects" lists module numbers:
1 FloorGen, 2 AI, 3 Player & Combat, 4 Progression, UI = HUD/menus.

## v1.2 — planned for the Alpha sprint (not in the repo yet)

1. `StatType` becomes the 9 Flow State stats, ordered Martial → Magic → Mental. `Archetype` is reordered to match and
   `StatRules.BelongsTo` divides by 3. *(3, 4)*
2. `IAbilityUser.RevokeAbility`, since upgrade nodes replace abilities. *(3, 4)*
3. `PlayerLeveledUp` event. *(4, UI)*
4. XP-reward interface for units. *(2, 4)*
5. Accuracy in the combat data. *(2, 3)*
6. Armor/Shroud absorption (Durability + Limit) in damage resolution. *(3, 4)*
7. `Faction` lives only in Core (FloorGen's own copy goes away). *(1, 2)*
8. `IFloorQuery.GetMoveSpeedMultiplier(Vector2Int cell)` for water. *(1, 2, 3)*
9. `SpawnerDestroyed(Faction, Vector3)` event; the exit spawns on the first one. *(1 or Game, 2)*
10. Gold/score event (e.g. `GoldCollected(int)`) plus a gold-drop hook on `ActorKilled`. *(2, 3, UI)*
11. `ICombatState { bool InCombat { get; } }` so the station and UI can read combat state without referencing Player. *(3 owns it; 4, UI read it)*

Open question for v1.2: where the shared `Health` component lives. If it stays in Player, AI units can't use it
(AI may only reference Core). Options: move `Health` into Core, or have AI implement `IDamageable` itself.

## v1.1 — contract from the Alpha plan

Faction and ChunkIntensity enums; `DamageType`, `DamageInfo`, `IDamageable`; `Archetype`, placeholder `StatType`,
`StatRules`, `StatModifier`, `IStatProvider`; `AbilityDefinition`, `IAbilityUser`; `SpawnPoint`, `IFloorQuery`;
`GameEvents` (`FloorGenerated`, `ActorKilled`, `PlayerDied`). Superseded by v1.2 before it was committed.
