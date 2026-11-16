using UnityEngine;
using System.Collections;

namespace DebtDungeon.Player
{
    public enum StatusEffect
    {
        Slowed,
        Poisoned,
        Strengthened1,
        Strengthened2,
        Strengthened3,
        Weakened1,
        Weakened2,
        Weakened3
    }

    public class StatusEffects : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        private Coroutine _slowCoroutine;
        [SerializeField] private float _slowDuration = 2f;

        public void ApplyStatus(StatusEffect statusEffect)
        {

            switch (statusEffect)
            {
                case StatusEffect.Slowed:

                    // Stop any already running slow coroutine
                    if (_slowCoroutine != null)
                    {
                        StopCoroutine(_slowCoroutine);
                    }

                    // Start a new slow coroutine for timer
                    _playerController._terrainMultiplier = 0.5f;
                    _slowCoroutine = StartCoroutine(SlowedTimer(_slowDuration));
                    break;
            }
        }

        public void RemoveStatus(StatusEffect statusEffect)
        {
            switch (statusEffect)
            {
                case StatusEffect.Slowed:
                    StopCoroutine(_slowCoroutine);
                    _playerController._terrainMultiplier = 1f;
                    break;
            }
        }

        private IEnumerator SlowedTimer(float _slowDuration)
        {
            yield return new WaitForSeconds(_slowDuration);
            RemoveStatus(StatusEffect.Slowed);
        }
    }
}
