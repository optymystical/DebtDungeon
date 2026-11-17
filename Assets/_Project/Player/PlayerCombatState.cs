using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace DebtDungeon.Player
{
    public class PlayerCombatState : MonoBehaviour, ICombatState
    {
        [FormerlySerializedAs("_Health")]
        [SerializeField] private Health _health;
        private bool _inCombat = false;
        public bool InCombat => _inCombat;
        private Coroutine _myCoroutine;

        private void Awake()
        {
            // Not wired on the player prefab, so fall back to this object's Health.
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            _health.Damaged += EnterCombat;
        }

        private void OnDisable()
        {
            _health.Damaged -= EnterCombat;

            // Disabling stops the timer coroutine, so reset here or InCombat could stay true forever.
            _inCombat = false;
            _myCoroutine = null;
        }

        private void EnterCombat(DamageInfo info, int amount)
        {
            MarkInCombat();
        }

        /// <summary>Starts or restarts the 5 s in-combat timer. Call this from attack code when the player attacks.</summary>
        public void MarkInCombat()
        {
            _inCombat = true;

            // Stop any already running coroutines
            if (_myCoroutine != null)
            {
                StopCoroutine(_myCoroutine);
            }

            // Start a new coroutine for timer
            _myCoroutine = StartCoroutine(ResetTimer());
        }

        private IEnumerator ResetTimer()
        {
            yield return new WaitForSeconds(5f);
            _inCombat = false;
        }
    }
}
