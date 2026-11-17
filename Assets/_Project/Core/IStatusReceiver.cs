using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// Anything that can have a status effect put on it. Player implements it (StatusEffects) and so do AI units
    /// (StatusEffectReceiver2D). Projectiles, traps and abilities find it with GetComponent&lt;IStatusReceiver&gt;()
    /// on whatever they hit, so they never need to reference another module.
    /// </summary>
    public interface IStatusReceiver
    {
        /// <summary>
        /// Applies one status effect.
        /// </summary>
        /// <param name="type">Which effect.</param>
        /// <param name="duration">Seconds it lasts. Non-stacking effects keep whichever end time is later.</param>
        /// <param name="magnitude">
        /// Slowed: the move speed multiplier (0.5 = half speed).
        /// Poisoned: damage per tick (ticks every 1 second).
        /// Strengthened / Weakened: ignored; stacks decide the strength.
        /// </param>
        /// <param name="source">What applied it, for kill credit (e.g. the spider). Can be null.</param>
        /// <param name="sourceFaction">The applier's faction. Poison damage uses it, so same-faction poison does nothing.</param>
        /// <param name="stacks">How many stacks to add. Only Strengthened and Weakened use it; pass 1 otherwise.</param>
        void ApplyStatus(StatusEffectType type, float duration, float magnitude, GameObject source, Faction sourceFaction, int stacks);
    }
}
