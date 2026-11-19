using System.Collections;
using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// A 360° melee spin around the player (Whirlwind, Blender). Hits every enemy within the weapon's reach,
    /// once per spin, for a multiple of the current basic attack's damage. Blender is the same shape with 2 hits.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_", menuName = "Debt Dungeon/Abilities/Spin Attack")]
    public class SpinAttackDefinition : SkillDefinition
    {
        [Header("Spin")]
        [Tooltip("How many times it hits. Whirlwind 1, Blender 2.")]
        [SerializeField, Min(1)] private int _hits = 1;

        [Tooltip("Seconds between hits when Hits is more than 1.")]
        [SerializeField, Min(0f)] private float _timeBetweenHits = 0.25f;

        [Tooltip("Damage per hit as a multiple of the basic attack's damage (1 = the same).")]
        [SerializeField, Min(0f)] private float _damageMultiplier = 1f;

        [Tooltip("Reach used if the basic attack isn't a melee attack (normally the weapon's range + radius is used).")]
        [SerializeField, Min(0.1f)] private float _fallbackReach = 1.5f;

        [Tooltip("Ring shown around the player on each hit. 32 px at 16 PPU (2 units across).")]
        [SerializeField] private Sprite _spinSprite;

        public override void Execute(SkillContext context)
        {
            // Reach: as far as the weapon's basic attack can hit (its circle's far edge).
            float reach = _fallbackReach;
            if (context.Runner.CurrentBasicAttack is MeleeAttackDefinition melee)
            {
                reach = melee.Range + melee.Radius;
            }

            int damage = Mathf.RoundToInt(context.Runner.GetBasicAttackDamage() * _damageMultiplier);
            context.Runner.StartCoroutine(SpinRoutine(context, reach, damage));
        }

        private IEnumerator SpinRoutine(SkillContext context, float reach, int damage)
        {
            for (int hit = 0; hit < _hits; hit++)
            {
                if (context.User == null)
                {
                    yield break;
                }

                HitEverythingInReach(context, reach, damage);

                if (hit < _hits - 1)
                {
                    yield return new WaitForSeconds(_timeBetweenHits);
                }
            }
        }

        private void HitEverythingInReach(SkillContext context, float reach, int damage)
        {
            Vector2 center = context.User.transform.position;
            SwingEffect.Spawn(_spinSprite, center, Vector2.right, reach);

            // One enemy can have several colliders, so remember who was already hit by this spin.
            HashSet<Health> alreadyHit = new HashSet<Health>();
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, reach);
            foreach (Collider2D collider in colliders)
            {
                Health target = collider.GetComponentInParent<Health>();
                if (target == null || !target.IsAlive || target.Faction == context.UserFaction || alreadyHit.Contains(target))
                {
                    continue;
                }

                alreadyHit.Add(target);
                HitResult result = CombatRules.RollHit(context.Accuracy);
                if (result != HitResult.Miss)
                {
                    target.TakeDamage(new DamageInfo(CombatRules.GetDamage(damage, result), DamageType.Physical,
                        context.User, context.UserFaction, result == HitResult.Critical));
                }
            }
        }
    }
}
