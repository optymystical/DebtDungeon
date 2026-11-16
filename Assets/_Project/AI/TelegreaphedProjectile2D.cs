using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Slow, readable projectile with optional ground landing circle, arc hop, AoE impact,
    /// and Control effects (Slow + togglable Poison).
    /// </summary>
    public class TelegraphedProjectile2D : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _projectileSprite;
        [SerializeField] private Transform _groundWarningRing;
        [SerializeField] private float _arcHeight = 0.85f;

        private Vector2 _startPos;
        private Vector2 _targetPos;
        private float _flightDuration;
        private float _elapsed;

        private GameObject _owner;
        private Faction _ownerFaction;
        private UnitConfig _config;
        private LayerMask _targetMask;
        private bool _initialized;

        private static readonly Collider2D[] ImpactBuffer = new Collider2D[16];

        public void Initialize(
            Vector2 startPos,
            Vector2 targetPos,
            GameObject owner,
            Faction ownerFaction,
            UnitConfig config,
            LayerMask targetMask)
        {
            _startPos = startPos;
            _targetPos = targetPos;
            _owner = owner;
            _ownerFaction = ownerFaction;
            _config = config;
            _targetMask = targetMask;

            float speed = Mathf.Max(1f, config.ProjectileSpeed);
            float distance = Vector2.Distance(startPos, targetPos);
            _flightDuration = Mathf.Max(0.25f, distance / speed);
            _elapsed = 0f;
            _initialized = true;

            transform.position = startPos;

            if (_groundWarningRing != null)
            {
                bool showRing = config.ShowGroundWarningCircle && config.AoERadius > 0.05f;
                _groundWarningRing.gameObject.SetActive(showRing);
                if (showRing)
                {
                    // Detach or position in world space at the exact landing zone
                    _groundWarningRing.SetParent(null);
                    _groundWarningRing.position = targetPos;
                    float diameter = config.AoERadius * 2f;
                    _groundWarningRing.localScale = new Vector3(diameter, diameter, 1f);
                }
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightDuration);

            Vector2 flatPos = Vector2.Lerp(_startPos, _targetPos, t);
            float heightOffset = (_config.Style == AttackStyle.RangedSpitter)
                ? Mathf.Sin(t * Mathf.PI) * _arcHeight
                : 0f;

            transform.position = new Vector3(flatPos.x, flatPos.y + heightOffset, transform.position.z);

            if (t >= 1f)
            {
                DetonateAtTarget();
            }
        }

        private void DetonateAtTarget()
        {
            float radius = Mathf.Max(0.45f, _config.AoERadius);
            int hits = Physics2D.OverlapCircleAll(_targetPos, radius, _targetMask).Length;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(_targetPos, radius, _targetMask);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col == null)
                {
                    continue;
                }

                Health targetHealth = col.GetComponentInParent<Health>();
                if (targetHealth == null || !targetHealth.IsAlive || targetHealth.Faction == _ownerFaction)
                {
                    continue;
                }

                // 1. Direct / AoE Impact Damage
                DamageInfo impact = new DamageInfo(
                    _config.AttackDamage,
                    _config.DamageType,
                    _owner,
                    _ownerFaction,
                    false);
                targetHealth.TakeDamage(impact);

                // 2. Knockback (for Goblin Rocks)
                if (_config.KnockbackForce > 0f && targetHealth.TryGetComponent(out CombatAgent2D targetAgent))
                {
                    Vector2 pushDir = ((Vector2)targetHealth.transform.position - _targetPos).normalized;
                    if (pushDir == Vector2.zero)
                    {
                        pushDir = (_targetPos - _startPos).normalized;
                    }
                    targetAgent.QueueKnockback(pushDir, _config.KnockbackForce);
                }

                // 3. Control Effects (Web Weaver Slow + Togglable Poison)
                if (_config.Style == AttackStyle.ControlWeaver)
                {
                    StatusEffectReceiver2D receiver = targetHealth.GetComponent<StatusEffectReceiver2D>();
                    if (receiver == null)
                    {
                        receiver = targetHealth.gameObject.AddComponent<StatusEffectReceiver2D>();
                    }

                    receiver.ApplySlow(_config.SlowMultiplier, _config.SlowDurationSeconds);

                    if (_config.ApplyPoison)
                    {
                        receiver.ApplyPoison(
                            _config.PoisonTickDamage,
                            _config.PoisonTickInterval,
                            _config.PoisonDurationSeconds,
                            _owner,
                            _ownerFaction);
                    }
                }
            }

            NoiseEventBus.EmitNoise(_targetPos, 6f);

            if (_groundWarningRing != null)
            {
                Destroy(_groundWarningRing.gameObject);
            }

            Destroy(gameObject);
        }
    }
}
