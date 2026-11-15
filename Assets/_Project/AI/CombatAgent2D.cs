using UnityEngine;
using System;
using System.Collections;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Bridges AI actors with Core's Health component, handling knockback,
    /// hit flashing, GameObject cleanup on death, and IKillReward telemetry.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class CombatAgent2D : MonoBehaviour, IKillReward
    {
        private const float HitFlashDuration = 0.1f;

        [Header("Configuration")]
        [SerializeField] private UnitConfig _stats;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer _visualRenderer;

        private Rigidbody2D _rigidbody;
        private Health _health;
        private Coroutine _flashCoroutine;
        private Vector2 _pendingKnockbackImpulse;

        public event Action<GameObject> OnAttackedBy;

        public Faction Faction => _health != null ? _health.Faction : Faction.Neutral;
        public UnitConfig Stats => _stats;
        public Rigidbody2D Rigidbody => _rigidbody;
        public Health Health => _health;
        public SpriteRenderer VisualRenderer => _visualRenderer;

        public bool IsAlive => _health != null && _health.IsAlive;
        public int CurrentHealth => _health != null ? _health.CurrentHealth : 0;
        public int MaxHealth => _health != null ? _health.MaxHealth : 1;

        // IKillReward Implementation
        public string DisplayName => _stats != null && !string.IsNullOrWhiteSpace(_stats.DisplayName)
            ? _stats.DisplayName
            : gameObject.name;

        public int XpReward => _stats != null ? _stats.XpReward : 10;
        public int GoldReward => _stats != null ? _stats.GoldDropAmount : 5;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();

            ConfigureRigidbodyForTopDown();

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += HandleHealthDamaged;
                _health.Died += HandleHealthDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleHealthDamaged;
                _health.Died -= HandleHealthDied;
            }
        }

        public void Initialize(UnitConfig stats)
        {
            _stats = stats;

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public void QueueKnockback(Vector2 hitDirection, float force)
        {
            if (force > 0f && hitDirection.sqrMagnitude > 0.001f)
            {
                _pendingKnockbackImpulse = hitDirection.normalized * force;
            }
        }

        private void HandleHealthDamaged(DamageInfo info, int damageTaken)
        {
            if (_rigidbody != null)
            {
                if (_pendingKnockbackImpulse.sqrMagnitude > 0.001f)
                {
                    _rigidbody.AddForce(_pendingKnockbackImpulse, ForceMode2D.Impulse);
                    _pendingKnockbackImpulse = Vector2.zero;
                }
                else if (info.Source != null)
                {
                    Vector2 fallbackDir = ((Vector2)transform.position - (Vector2)info.Source.transform.position).normalized;
                    float fallbackForce = _stats != null ? _stats.KnockbackForce : 5f;
                    _rigidbody.AddForce(fallbackDir * fallbackForce, ForceMode2D.Impulse);
                }
            }

            TriggerHitFlash();

            if (info.Source != null)
            {
                OnAttackedBy?.Invoke(info.Source);
            }
        }

        private void HandleHealthDied(KillInfo kill)
        {
            Destroy(gameObject);
        }

        private void ConfigureRigidbodyForTopDown()
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = 0f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void TriggerHitFlash()
        {
            if (_visualRenderer == null)
            {
                return;
            }

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            Color originalColor = _visualRenderer.color;
            _visualRenderer.color = Color.red;

            yield return new WaitForSeconds(HitFlashDuration);

            if (_visualRenderer != null)
            {
                _visualRenderer.color = originalColor == Color.red ? Color.white : originalColor;
            }

            _flashCoroutine = null;
        }
    }
}
