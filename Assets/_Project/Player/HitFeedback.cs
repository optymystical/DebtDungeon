using UnityEngine;
using DebtDungeon.Core;
using System.Collections;

namespace DebtDungeon.Player
{
    public class HitFeedback : MonoBehaviour
    {
        [SerializeField] private Health _eventRaiser;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Rigidbody2D _rigidBody;
        [SerializeField] private float _knockbackForce = 1f;
        [SerializeField] private float _knockbackDelay = 0.2f;
        [SerializeField] private bool _isKnockingBack = false;
        public bool IsKnockingBack => _isKnockingBack;

        public void PlayerHit(DamageInfo info, int damage)
        {
            FlashSprite();
            Knockback(info.Source);
        }

        private void OnEnable()
        {
            _eventRaiser.Damaged += PlayerHit;
        }

        private void OnDisable()
        {
            _eventRaiser.Damaged -= PlayerHit;
        }

        private void FlashSprite()
        {
            Color original = _spriteRenderer.color;
            StartCoroutine(ChangeColor(0.1f, Color.red));
            StartCoroutine(ChangeColor(0.2f, original));
        }
        private IEnumerator ChangeColor(float delayTime, Color color)
        {
            yield return new WaitForSeconds(delayTime);
            _spriteRenderer.color = color;
        }

        private void Knockback(GameObject source)
        {
            _isKnockingBack = true;

            Vector2 direction = (transform.position - source.transform.position).normalized;
            _rigidBody.AddForce(direction * _knockbackForce, ForceMode2D.Impulse);

            StartCoroutine(resetKnockback());
        }

        private IEnumerator resetKnockback()
        {
            yield return new WaitForSeconds(_knockbackDelay);
            _isKnockingBack = false;
        }
    }
}
