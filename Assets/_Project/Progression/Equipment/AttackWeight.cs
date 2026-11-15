namespace DebtDungeon.Progression
{
    /// <summary>
    /// Whether an item's basic attack is light (fast) or heavy (slow, strong, bigger hitbox or projectile).
    /// Martial: Light and Heavy weapons. Magic: Igniter Foci are light, Channeler Foci are heavy.
    /// The timing itself (windup, cooldown, hitbox size) lives on the basic-attack ability the item grants.
    /// </summary>
    public enum AttackWeight
    {
        Light = 0,
        Heavy = 1
    }
}
