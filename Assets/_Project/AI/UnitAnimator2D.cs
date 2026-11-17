using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Drives directional 2D sprite animations, horizontal flipping, attack telegraphs,
    /// hurt flinches, and death playback from Rigidbody2D velocity and Core Health events.
    /// </summary>
    [RequireComponent(typeof(CombatAgent2D))]
    public class UnitAnimator2D : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Death Playback")]
        [Tooltip("Delay before destroying the GameObject so the death clip finishes playing.")]
        [SerializeField, Min(0f)] private float _deathAnimDuration = 0.65f;

        private Rigidbody2D _rb;
        private Health _health;
        private Vector2 _lastFacing = Vector2.down;
        private bool _isDead;

        public float DeathAnimDuration => _deathAnimDuration;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += HandleDamaged;
                _health.Died += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= HandleDamaged;
                _health.Died -= HandleDied;
            }
        }

        private void Update()
        {
            if (_isDead || _animator == null || _rb == null)
            {
                return;
            }

            Vector2 vel = _rb.linearVelocity;
            float speed = vel.magnitude;

            if (speed > 0.05f)
            {
                _lastFacing = vel.normalized;
            }

            _animator.SetFloat(SpeedHash, speed);
            _animator.SetFloat(MoveXHash, _lastFacing.x);
            _animator.SetFloat(MoveYHash, _lastFacing.y);

            // Flip right-facing side sprites when moving/facing left
            if (_spriteRenderer != null && Mathf.Abs(_lastFacing.x) > 0.1f)
            {
                _spriteRenderer.flipX = _lastFacing.x < 0f;
            }
        }

        public void TriggerAttackAnimation()
        {
            if (!_isDead && _animator != null)
            {
                _animator.SetTrigger(AttackHash);
            }
        }

        private void HandleDamaged(DamageInfo info, int damageTaken)
        {
            if (!_isDead && _animator != null)
            {
                _animator.SetTrigger(HurtHash);
            }
        }

        private void HandleDied(KillInfo kill)
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }

            if (_animator != null)
            {
                _animator.SetTrigger(DieHash);
            }
        }
    }
}
