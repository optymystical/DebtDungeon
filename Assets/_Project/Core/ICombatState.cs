namespace DebtDungeon.Core
{
    /// <summary>
    /// Whether a character is in combat. Implemented by Player; read by the station, leveling and upgrade UI.
    /// In combat = being chased, attacking or being attacked, or within 5 s of taking damage.
    /// </summary>
    public interface ICombatState
    {
        /// <summary>True while the character counts as in combat.</summary>
        bool InCombat { get; }

        /// <summary>
        /// Starts or restarts the 5 s in-combat timer. Called by attack code, and by AI about once per second
        /// while a unit is chasing or attacking this character.
        /// </summary>
        void MarkInCombat();
    }
}
