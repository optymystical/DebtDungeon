namespace DebtDungeon.Core
{
    /// <summary>
    /// Armor, a Shroud or anything else that soaks up part of a hit before it reaches Health.
    /// Register one with Health.AddAbsorber. Implemented by Progression (equipment).
    /// </summary>
    public interface IDamageAbsorber
    {
        /// <summary>
        /// Absorbs what it can from the hit and returns the damage left over (0 to damage.Amount).
        /// Armor: absorbs up to its Limit, loses that much Durability, Physical only.
        /// Shroud: same, but also blocks Magic. Nothing absorbs Poison.
        /// </summary>
        int AbsorbDamage(DamageInfo damage);
    }
}
