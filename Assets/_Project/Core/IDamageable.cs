namespace DebtDungeon.Core
{
    /// <summary>Anything that can take damage. Core's Health component implements it.</summary>
    public interface IDamageable
    {
        /// <summary>False once the object has died.</summary>
        bool IsAlive { get; }

        /// <summary>Who this belongs to. Used for targeting and to ignore friendly fire.</summary>
        Faction Faction { get; }

        /// <summary>Applies one hit.</summary>
        void TakeDamage(DamageInfo damage);
    }
}
