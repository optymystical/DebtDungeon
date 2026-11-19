using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// Brace: every hit taken is reduced by a flat amount while it lasts (no durability; Poison goes through).
    /// Plain C# class: AbilityRunner registers it on the player's Health and removes it when time runs out.
    /// </summary>
    public class BraceBarrier : IDamageAbsorber
    {
        private readonly int _reductionPerHit;

        public BraceBarrier(int reductionPerHit)
        {
            _reductionPerHit = reductionPerHit;
        }

        public int AbsorbDamage(DamageInfo damage)
        {
            if (damage.Type == DamageType.Poison)
            {
                return damage.Amount;
            }

            return Mathf.Max(0, damage.Amount - _reductionPerHit);
        }
    }
}
