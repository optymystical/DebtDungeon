using DebtDungeon.Core;
using UnityEngine;
using System.Collections;

namespace DebtDungeon.Player
{
    public class PlayerCombatState : MonoBehaviour, ICombatState
    {
        [SerializeField] private Health _health;
        private bool _inCombat = false;
        public bool InCombat => _inCombat;
        private Coroutine _myCoroutine;

        private void OnEnable()
        {
            _health.Damaged += EnterCombat;
        }

        private void OnDisable()
        {
            _health.Damaged -= EnterCombat;

            // Reseting 
            _inCombat = false;
            _myCoroutine = null;
        }

        private void EnterCombat(DamageInfo info, int amount)
        {
            MarkInCombat();
        }

        // Starts or restarts the 5 sec in-combat timer. Call this from attack code when the player attacks
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
