using UnityEngine;
using UnityEngine.InputSystem;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Handles top-down WASD/arrow player movement and attacks for sandbox testing.
    /// </summary>
    [RequireComponent(typeof(CombatAgent2D))]
    public class Player2D : MonoBehaviour
    {
        private const float MeleeAttackRadius = 1.4f;
        private const float AttackOffsetDistance = 0.8f;
        private const float DefaultMoveSpeed = 5.0f;
        private const int DefaultAttackDamage = 10;
        private const float DefaultKnockback = 7f;

        [Header("Combat Settings")]
        [Tooltip("Layer mask containing hostile enemies that can be hit by melee strikes.")]
        [SerializeField] private LayerMask _enemyMask;

        [Tooltip("Delay in seconds between player attacks.")]
        [SerializeField, Min(0.1f)] private float _attackCooldown = 0.4f;

        private CombatAgent2D _combatAgent;
        private Vector2 _moveInput;
        private Vector2 _facingDirection = Vector2.down;
        private float _nextAttackTime;

        public Vector2 FacingDirection => _facingDirection;

        private void Awake()
        {
            _combatAgent = GetComponent<CombatAgent2D>();
        }

        private void Update()
        {
            if (!_combatAgent.IsAlive)
            {
                _moveInput = Vector2.zero;
                return;
            }

            ReadMovementInput();
            HandleMeleeInput();
        }

        private void FixedUpdate()
        {
            if (!_combatAgent.IsAlive)
            {
                return;
            }

            float speed = _combatAgent.Stats != null ? _combatAgent.Stats.MoveSpeed : DefaultMoveSpeed;
            _combatAgent.Rigidbody.linearVelocity = _moveInput * speed;
        }

        private void ReadMovementInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            _moveInput = new Vector2(horizontal, vertical).normalized;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _facingDirection = _moveInput;
            }
        }

        private void HandleMeleeInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame && Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + _attackCooldown;
                ExecuteMeleeStrike();
            }
        }

        private void ExecuteMeleeStrike()
        {
            Vector2 attackPoint = (Vector2)transform.position + _facingDirection * AttackOffsetDistance;
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint, MeleeAttackRadius, _enemyMask);

            int baseDamage = _combatAgent.Stats != null ? _combatAgent.Stats.AttackDamage : DefaultAttackDamage;
            DamageType damageType = _combatAgent.Stats != null ? _combatAgent.Stats.DamageType : DamageType.Physical;
            int accuracy = _combatAgent.Stats != null ? _combatAgent.Stats.Accuracy : CombatRules.BaseAccuracy;
            float knockback = _combatAgent.Stats != null ? _combatAgent.Stats.KnockbackForce : DefaultKnockback;

            for (int i = 0; i < hitColliders.Length; i++)
            {
                if (hitColliders[i].gameObject == gameObject)
                {
                    continue;
                }
                if (!hitColliders[i].TryGetComponent(out IDamageable targetDamageable))
                {
                    continue;
                }

                HitResult hitResult = CombatRules.RollHit(accuracy);
                if (hitResult == HitResult.Miss)
                {
                    continue;
                }

                int finalDamage = CombatRules.GetDamage(baseDamage, hitResult);
                bool isCritical = hitResult == HitResult.Critical;

                if (hitColliders[i].TryGetComponent(out CombatAgent2D enemyAgent))
                {
                    Vector2 hitDir = ((Vector2)enemyAgent.transform.position - (Vector2)transform.position).normalized;
                    enemyAgent.QueueKnockback(hitDir, knockback);
                }

                DamageInfo info = new DamageInfo(finalDamage, damageType, gameObject, _combatAgent.Faction, isCritical);
                targetDamageable.TakeDamage(info);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector2 attackPoint = (Vector2)transform.position + _facingDirection * 0.8f;
            Gizmos.DrawWireSphere(attackPoint, MeleeAttackRadius);
        }
    }
}
