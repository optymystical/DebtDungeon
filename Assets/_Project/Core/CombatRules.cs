using UnityEngine;

namespace DebtDungeon.Core
{
    /// <summary>
    /// Shared hit and crit math, used by the player and enemies alike so both follow the same rules.
    /// Accuracy is a whole-number percentage: 100 = a normal attack.
    /// </summary>
    public static class CombatRules
    {
        /// <summary>Accuracy every attack starts with before abilities and effects change it.</summary>
        public const int BaseAccuracy = 100;

        /// <summary>A critical hit deals this many times the normal damage (GDD: Accuracy and crits).</summary>
        public const int CriticalDamageMultiplier = 2;

        private const int PercentMax = 100;

        /// <summary>
        /// Rolls an attack. Hit chance is min(Accuracy, 100%). Any Accuracy above 100% becomes crit chance,
        /// capped at 100% (so 200% or more always crits).
        /// </summary>
        public static HitResult RollHit(int accuracy)
        {
            int hitChance = Mathf.Clamp(accuracy, 0, PercentMax);
            if (!RollPercent(hitChance))
            {
                return HitResult.Miss;
            }

            // Crit chance is only the part of Accuracy above 100%. TODO(Chris): revisit the cap in Beta.
            int critChance = Mathf.Clamp(accuracy - PercentMax, 0, PercentMax);
            if (RollPercent(critChance))
            {
                return HitResult.Critical;
            }

            return HitResult.Hit;
        }

        /// <summary>Turns base damage into final damage for a roll: 0 on a miss, double on a crit.</summary>
        public static int GetDamage(int baseDamage, HitResult result)
        {
            switch (result)
            {
                case HitResult.Miss:
                    return 0;
                case HitResult.Critical:
                    return baseDamage * CriticalDamageMultiplier;
                default:
                    return baseDamage;
            }
        }

        /// <summary>True with the given chance. Random.Range(0, 100) returns 0 to 99, so a chance of 100 always passes.</summary>
        private static bool RollPercent(int chance)
        {
            return Random.Range(0, PercentMax) < chance;
        }
    }
}
