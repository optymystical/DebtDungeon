namespace DebtDungeon.Core
{
    /// <summary>
    /// Where an ability goes in the player's controls. The ability runner (Player) slots abilities by this.
    /// </summary>
    public enum AbilityKind
    {
        /// <summary>Comes from the equipped weapon, Foci or Icon. Right-click.</summary>
        BasicAttack = 0,

        /// <summary>Parry, Ward and similar. Q.</summary>
        Defensive = 1,

        /// <summary>Normal skill-tree abilities. Keys 1-5.</summary>
        Active = 2,

        /// <summary>Always-on effects. No key, no cooldown.</summary>
        Passive = 3
    }
}
