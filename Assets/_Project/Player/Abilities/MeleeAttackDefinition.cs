using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class MeleeAttackDefinition : BasicAttackDefinition
    {
        private readonly float _range;
        private readonly float _radius;

        public override void Execute(GameObject user, Faction userFaction, Vector2 direction, int damage, int accuracy)
        {
            // Find all colliders hit
            Collider2D[] colliders = Physics2D.OverlapCircleAll((Vector2)user.transform.position + direction * _range, _radius);
            foreach (Collider2D collider in colliders)
            {
                // Only attack valid colliders
                Health health = collider.GetComponentInParent<Health>();
                if (health != null && collider.gameObject.transform.parent.gameObject != user && health.IsAlive
                    && health.Faction != Faction.Player)
                {
                    HitResult result = CombatRules.RollHit(accuracy);
                    if (result != HitResult.Miss)
                    {
                        health.TakeDamage(new DamageInfo(CombatRules.GetDamage(damage, result), _damageType, user,
                            userFaction, result == HitResult.Critical));
                    }
                }
            }
        }
    }
}
