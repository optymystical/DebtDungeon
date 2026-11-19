namespace DebtDungeon.Core
{
    /// <summary>
    /// Core v1.7. Something on a defender that changes how easily it is hit (Close Quarters: -20 vs melee).
    /// The attacker adds the result to its own Accuracy through CombatRules.RollHitAgainst.
    /// Implemented by Player's AbilityRunner (from passives). Enemies don't need one.
    /// </summary>
    public interface IDefenseModifier
    {
        /// <summary>Accuracy added to attacks against this defender (negative = harder to hit).</summary>
        int GetDefenderAccuracyModifier(bool isMeleeAttack);
    }
}
