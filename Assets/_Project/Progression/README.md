# Progression (Module 4) — `DebtDungeon.Progression`

**Owner:** Christopher · **Branch:** `feature/chris-progression`
**Sandbox scene:** `Scenes/Sandbox/Chris_Progression.unity`
**Data:** `Data/Presets/`, `Data/SkillTrees/`, `Data/Items/` (equipment) · **Prefabs:** `Prefabs/Progression/`
**References:** Core, uGUI, TextMeshPro.

The Module Week prototype is a separate project (`C:\Unity\DebtDungeon_Module4`). Reference only; rewrite here.

## Alpha scope
- Archetypes: **Martial and Magic only** (Mental is Beta). All 9 stats exist; a character uses only its own
  archetype's 3.
- Skill trees, nodes 1–3 only: Bladed Weapons and Medium Armor (Martial); Radiation and Protection Arcana (Magic).
  Trees can be bought without their equipment, but abilities need it to be used.
- Equipment: Martial has 2 melee weapons (Light, Heavy) and 1 armor; Magic has 1 Foci, 1 Shroud and 2–3 simple Affixes.
- Loadout is picked at character creation. The spawn-chunk station is the only place to change it (small pop-up).
- Power = stat ÷ 10. Max HP comes from the defensive stat. 3 stat points and 1 skill point per level; cap 10 (temporary).
- Leveling up and buying upgrades only out of combat (`ICombatState`). Changing hotkeys is allowed anytime.
- Beta, not now: Mental, inventory, consumables, loot.

## Talks to other modules through Core
Implements `IStatProvider`; grants abilities through `IAbilityUser`; listens to `ActorKilled` for XP; raises
`PlayerLeveledUp` (v1.2).
