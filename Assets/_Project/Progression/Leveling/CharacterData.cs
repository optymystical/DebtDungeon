using DebtDungeon.Core;

namespace DebtDungeon.Progression
{
    /// <summary>
    /// Everything about the player's character in one object: name, stats, loadout and progress.
    /// The creator builds it from a preset; Game carries it from the MainMenu scene into the run.
    /// </summary>
    public class CharacterData
    {
        private const string DefaultName = "Unnamed Debtor";

        private CharacterData(string name, CharacterStats stats, Loadout loadout, CharacterProgress progress, CharacterSkills skills)
        {
            Name = name;
            Stats = stats;
            Loadout = loadout;
            Progress = progress;
            Skills = skills;
        }

        /// <summary>The character's name. A blank name becomes "Unnamed Debtor".</summary>
        public string Name { get; }

        /// <summary>The character's archetype.</summary>
        public Archetype Archetype => Stats.Archetype;

        /// <summary>Base stats plus modifiers. Other modules read these as IStatProvider.</summary>
        public CharacterStats Stats { get; }

        /// <summary>Worn gear.</summary>
        public Loadout Loadout { get; }

        /// <summary>Level, XP and unspent points.</summary>
        public CharacterProgress Progress { get; }

        /// <summary>Skill tree nodes bought.</summary>
        public CharacterSkills Skills { get; }

        /// <summary>
        /// Spends one stat point to raise one of the character's stats by 1. Refused (with a reason for the UI) while
        /// in combat, for another archetype's stat, or with no points left. Level-ups happen anytime; only spending
        /// waits for out of combat (decided Nov 9). Pass null for combatState where there is no combat (e.g. the creator).
        /// </summary>
        public bool TrySpendStatPoint(StatType stat, ICombatState combatState, out string refusalReason)
        {
            if (combatState != null && combatState.InCombat)
            {
                refusalReason = "Can't spend points while in combat.";
                return false;
            }

            if (!StatRules.BelongsTo(stat, Archetype))
            {
                refusalReason = $"{stat} isn't a {Archetype} stat.";
                return false;
            }

            if (!Progress.TryUseStatPoint())
            {
                refusalReason = "No stat points to spend.";
                return false;
            }

            Stats.SetBaseStat(stat, Stats.GetBaseStat(stat) + 1);
            refusalReason = null;
            return true;
        }

        /// <summary>Builds a level-1 character from a preset: its stats, its default gear and a fresh progress track.</summary>
        public static CharacterData CreateFromPreset(PresetDefinition preset, string characterName, LevelCurveDefinition curve)
        {
            CharacterStats stats = preset.CreateStats();
            Loadout loadout = new Loadout(stats);
            preset.TryApplyStartingLoadout(loadout);

            string name = string.IsNullOrWhiteSpace(characterName) ? DefaultName : characterName.Trim();
            CharacterProgress progress = new CharacterProgress(curve);
            CharacterSkills skills = new CharacterSkills(stats.Archetype, progress);
            return new CharacterData(name, stats, loadout, progress, skills);
        }
    }
}
