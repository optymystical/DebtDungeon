using UnityEngine;
using DebtDungeon.Core;
using System.Collections;

namespace DebtDungeon.Player
{
    public class HitFeedback : MonoBehaviour
    {
        [SerializeField] private Health _Health;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Rigidbody2D _rigidBody;
        [SerializeField] private float _knockbackForce = 1f;
        [SerializeField] private float _knockbackDelay = 0.5f;
        private Color _originalColor;
        private bool _isKnockingBack = false;
        private Coroutine _flashRoutine;
        private Coroutine _knockbackRoutine;
        public bool IsKnockingBack => _isKnockingBack;

        private void Awake()
        {
            _originalColor = _spriteRenderer.color;
        }
        public void PlayerHit(DamageInfo info, int damage)
        {
            FlashSprite();
            Knockback(info.Source);
        }

        private void OnEnable()
        {
            _Health.Damaged += PlayerHit;
        }

        private void OnDisable()
        {
            _Health.Damaged -= PlayerHit;
        }

        private void FlashSprite()
        {
            // Restart the flash on every hit, so an older hit's timer can't end the new flash early.
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _spriteRenderer.color = Color.red;
            _flashRoutine = StartCoroutine(ChangeColor(_knockbackDelay, _originalColor));
        }
        private IEnumerator ChangeColor(float delayTime, Color color)
        {
            yield return new WaitForSeconds(delayTime);
            _spriteRenderer.color = color;
        }

        private void Knockback(GameObject source)
        {
            if (source == null)
            {
                return;
            }

            _isKnockingBack = true;

            Vector2 direction = (transform.position - source.transform.position).normalized;
            _rigidBody.AddForce(direction * _knockbackForce, ForceMode2D.Impulse);

            if (_knockbackRoutine != null)
            {
                StopCoroutine(_knockbackRoutine);
            }

            _knockbackRoutine = StartCoroutine(ResetKnockback());
        }

        private IEnumerator ResetKnockback()
        {
            yield return new WaitForSeconds(_knockbackDelay);
            _isKnockingBack = false;
        }
    }
}
