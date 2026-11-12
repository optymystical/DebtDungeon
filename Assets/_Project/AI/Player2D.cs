using UnityEngine;
//using UnityEngine.InputSystem;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Handles top-down WASD/arrow player movement and attacks for sandbox testing.
    /// </summary>
    [RequireComponent(typeof(CombatAgent2D))]
    public class Player2D : MonoBehaviour
    {
        private const float MeleeAttackRadius = 1.4f;

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

            float speed = _combatAgent.Stats != null ? _combatAgent.Stats.MoveSpeed : 5.0f;
            _combatAgent.Rigidbody.linearVelocity = _moveInput * speed;
        }

        private void ReadMovementInput()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                vertical += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                vertical -= 1f;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
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
            if (Input.GetKeyDown(KeyCode.Space) && Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + _attackCooldown;
                ExecuteMeleeStrike();
            }
        }

        private void ExecuteMeleeStrike()
        {
            Vector2 attackPoint = (Vector2)transform.position + _facingDirection * 0.8f;
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackPoint, MeleeAttackRadius, _enemyMask);

            int damage = _combatAgent.Stats != null ? _combatAgent.Stats.AttackDamage : 10;
            float knockback = _combatAgent.Stats != null ? _combatAgent.Stats.KnockbackForce : 7f;

            for (int i = 0; i < hitColliders.Length; i++)
            {
                CombatAgent2D enemy = hitColliders[i].GetComponent<CombatAgent2D>();
                if (enemy != null && enemy != _combatAgent && enemy.IsAlive)
                {
                    Vector2 hitDirection = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
                    enemy.ApplyDamage(damage, hitDirection, knockback, _combatAgent);
                }
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
