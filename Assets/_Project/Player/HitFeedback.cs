using UnityEngine;
using DebtDungeon.Core;
using System.Collections;

namespace DebtDungeon.Player
{
    public class HitFeedback : MonoBehaviour
    {
        [SerializeField] private Health _eventRaiser;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public void PlayerHit(DamageInfo info, int damage)
        {
            FlashSprite();
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
        protected IEnumerator ChangeColor(float delayTime, Color color)
        {
            yield return new WaitForSeconds(delayTime);
            _spriteRenderer.color = color;
        }
    }
}
