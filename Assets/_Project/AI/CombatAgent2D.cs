using UnityEngine;
using System;
using System.Collections;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Handles health, damage reception, knockback physics, and visual hit feedback
    /// for both AI units and the sandbox test player.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CombatAgent2D : MonoBehaviour
    {
        private const float HitFlashDuration = 0.1f;

        [Header("Configuration")]
        [SerializeField] private FactionDefinition _faction;
        [SerializeField] private UnitConfig _stats;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer _visualRenderer;

        private Rigidbody2D _rigidbody;
        private int _currentHealth;
        private int _maxHealth;
        private Coroutine _flashCoroutine;

        public event Action<CombatAgent2D> OnDamaged;
        public event Action<CombatAgent2D> OnDied;

        public FactionDefinition Faction => _faction;
        public UnitConfig Stats => _stats;
        public Rigidbody2D Rigidbody => _rigidbody;
        public SpriteRenderer VisualRenderer => _visualRenderer;

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            ConfigureRigidbodyForTopDown();

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            InitializeHealth();
        }

        /// <summary>
        /// Programmatically initializes faction, stats, and health when spawned dynamically.
        /// </summary>
        public void Initialize(FactionDefinition faction, UnitConfig stats)
        {
            _faction = faction;
            _stats = stats;

            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            InitializeHealth();
        }

        private void InitializeHealth()
        {
            _maxHealth = _stats != null ? Mathf.Max(1, _stats.MaxHealth) : 30;
            _currentHealth = _maxHealth;
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

        /// <summary>
        /// Applies damage, triggers directional knockback, and notifies listeners for AI retaliation.
        /// </summary>
        public void ApplyDamage(int amount, Vector2 hitDirection, float knockbackForce, CombatAgent2D attacker = null)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);

            // Apply physical knockback impulse
            if (_rigidbody != null && knockbackForce > 0f && hitDirection.sqrMagnitude > 0.001f)
            {
                _rigidbody.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);
            }

            TriggerHitFlash();
            OnDamaged?.Invoke(attacker);

            if (_currentHealth <= 0)
            {
                HandleDeath();
            }
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

        private void HandleDeath()
        {
            OnDied?.Invoke(this);

            // If a manager is active, notify it of the drop
            if (GoldDropManager.Instance != null && _stats != null)
            {
                GoldDropManager.Instance.SpawnGold(transform.position, _stats.GoldDropAmount);
            }

            Destroy(gameObject);
        }
    }
}
