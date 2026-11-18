using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    [CreateAssetMenu(fileName = "Attack_", menuName = "Debt Dungeon/Abilities/Melee Attack")]
    public class MeleeAttackDefinition : BasicAttackDefinition
    {
        [SerializeField] private float _range;
        [SerializeField] private float _radius;

        public float Range => _range;
        public float Radius => _radius;

        public override void Execute(GameObject user, Faction userFaction, Vector2 direction, int damage, int accuracy)
        {
            // Find all colliders hit
            Collider2D[] colliders = Physics2D.OverlapCircleAll((Vector2)user.transform.position + direction * _range, _radius);
            foreach (Collider2D collider in colliders)
            {
                // Only attack valid colliders
                Health targetsHealth = collider.GetComponentInParent<Health>();
                if (targetsHealth != null && collider.gameObject != user && targetsHealth.IsAlive
                    && targetsHealth.Faction != Faction.Player)
                {
                    HitResult result = CombatRules.RollHit(accuracy);
                    if (result != HitResult.Miss)
                    {
                        targetsHealth.TakeDamage(new DamageInfo(CombatRules.GetDamage(damage, result), DamageType, user,
                            userFaction, result == HitResult.Critical));
                    }
                }
            }
        }
    }
}
