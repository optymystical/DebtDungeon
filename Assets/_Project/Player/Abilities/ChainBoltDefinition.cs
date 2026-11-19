using System.Collections;
using System.Collections.Generic;
using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    /// <summary>
    /// Crackle and its upgrades: a bolt fired toward the mouse. If it hits, lightning jumps to the nearest enemy
    /// (within Chain Range of the last target hit) that this cast hasn't hit yet. Each jump rolls again with
    /// less Accuracy, and the chain stops at the first miss or when nobody is left in range.
    /// Crackle II (Electrify) makes every hit Strengthened (+50%). Crackle III (Lightning Rod) also zaps the first
    /// target each time a later jump deals damage.
    /// </summary>
    [CreateAssetMenu(fileName = "Ability_", menuName = "Debt Dungeon/Abilities/Chain Bolt")]
    public class ChainBoltDefinition : SkillDefinition
    {
        private static readonly Color MissColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);

        [Header("Damage")]
        [Tooltip("Damage per hit = this x Power of the Foci's stat (Reach for Scroll, Grasp for Wand).")]
        [SerializeField, Min(0f)] private float _damagePerPower = 8f;

        [Tooltip("Stat used if no basic attack is granted (normally the basic attack's stat is used).")]
        [SerializeField] private StatType _fallbackStat = StatType.Grasp;

        [Tooltip("Electrify: every hit is Strengthened (x1.5).")]
        [SerializeField] private bool _strengthenedHits;

        [Tooltip("Lightning Rod: the first target takes this x Power each time a later jump deals damage. 0 = off.")]
        [SerializeField, Min(0f)] private float _rodDamagePerPower;

        [Header("Chain")]
        [Tooltip("How far (units/tiles) a jump can reach from the last target hit.")]
        [SerializeField, Min(0.5f)] private float _chainRange = 4f;

        [Tooltip("Accuracy lost on each jump (stacks: -20, -40, -60...).")]
        [SerializeField, Min(0)] private int _accuracyLossPerJump = 20;

        [Tooltip("Safety cap. Falling Accuracy ends chains long before this.")]
        [SerializeField, Min(1)] private int _maxJumps = 20;

        [Tooltip("Seconds between jumps, so the chain is visible.")]
        [SerializeField, Min(0f)] private float _jumpDelay = 0.08f;

        [Header("Bolt")]
        [Tooltip("Projectile prefab for the first bolt (Matthew's WandBolt works).")]
        [SerializeField] private Projectile _boltPrefab;
        [SerializeField, Min(0.1f)] private float _boltSpeed = 9f;
        [SerializeField, Min(0.1f)] private float _boltLifetime = 1f;
        [SerializeField] private Color _boltColor = new Color(1f, 0.95f, 0.45f, 1f);

        [Tooltip("1 unit long line sprite for the jumps.")]
        [SerializeField] private Sprite _beamSprite;

        private const float StrengthenedMultiplier = 1.5f;

        public override void Execute(SkillContext context)
        {
            if (_boltPrefab == null)
            {
                Debug.LogWarning($"{name} has no Bolt Prefab.", this);
                return;
            }

            BasicAttackDefinition basicAttack = context.Runner.CurrentBasicAttack;
            StatType stat = basicAttack != null ? basicAttack.ScalingStat : _fallbackStat;
            float power = context.Runner.GetModifiedPower(stat);

            float damage = _damagePerPower * power;
            if (_strengthenedHits)
            {
                damage *= StrengthenedMultiplier;
            }
            int hitDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
            int rodDamage = Mathf.RoundToInt(_rodDamagePerPower * power);

            Vector2 spawnPosition = (Vector2)context.User.transform.position + context.Direction * 0.6f;
            Projectile bolt = Instantiate(_boltPrefab, spawnPosition, Quaternion.identity);

            SpriteRenderer boltRenderer = bolt.GetComponentInChildren<SpriteRenderer>();
            if (boltRenderer != null)
            {
                boltRenderer.color = _boltColor;
            }

            // The bolt does the first hit itself. When it reports back, a hit starts the chain from that target.
            // The lambda remembers context, hitDamage and rodDamage from this cast (a closure).
            bolt.Hit += (target, result) =>
            {
                if (result != HitResult.Miss && context.Runner != null)
                {
                    context.Runner.StartCoroutine(ChainRoutine(context, target, hitDamage, rodDamage));
                }
            };

            bolt.Launch(context.Direction * _boltSpeed, _boltLifetime, context.User, context.UserFaction, hitDamage,
                context.Accuracy, DamageType.Magic);
        }

        private IEnumerator ChainRoutine(SkillContext context, Health firstTarget, int hitDamage, int rodDamage)
        {
            // Everyone this cast has hit, so a chain never hits the same target twice.
            HashSet<Health> alreadyHit = new HashSet<Health> { firstTarget };

            // Positions are stored, not the Health, because a target can die and be destroyed mid-chain.
            Vector2 lastPosition = firstTarget.transform.position;
            int accuracy = context.Accuracy;

            for (int jump = 0; jump < _maxJumps; jump++)
            {
                yield return new WaitForSeconds(_jumpDelay);

                Health next = FindNearestTarget(lastPosition, alreadyHit, context.UserFaction);
                if (next == null)
                {
                    yield break;
                }

                accuracy -= _accuracyLossPerJump;
                HitResult result = CombatRules.RollHit(accuracy);
                Vector2 nextPosition = next.transform.position;

                if (result == HitResult.Miss)
                {
                    BeamEffect.Spawn(_beamSprite, lastPosition, nextPosition, MissColor);
                    yield break;
                }

                BeamEffect.Spawn(_beamSprite, lastPosition, nextPosition, _boltColor);
                next.TakeDamage(new DamageInfo(CombatRules.GetDamage(hitDamage, result), DamageType.Magic,
                    context.User, context.UserFaction, result == HitResult.Critical));

                // Lightning Rod: the first target is zapped again for every later jump that deals damage.
                if (rodDamage > 0 && firstTarget != null && firstTarget.IsAlive)
                {
                    firstTarget.TakeDamage(new DamageInfo(rodDamage, DamageType.Magic, context.User, context.UserFaction));
                }

                alreadyHit.Add(next);
                lastPosition = nextPosition;
            }
        }

        private Health FindNearestTarget(Vector2 from, HashSet<Health> alreadyHit, Faction userFaction)
        {
            Health nearest = null;
            float nearestDistance = float.MaxValue;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(from, _chainRange);
            foreach (Collider2D collider in colliders)
            {
                Health candidate = collider.GetComponentInParent<Health>();
                if (candidate == null || !candidate.IsAlive || candidate.Faction == userFaction
                    || alreadyHit.Contains(candidate))
                {
                    continue;
                }

                float distance = Vector2.Distance(from, candidate.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }
    }
}
