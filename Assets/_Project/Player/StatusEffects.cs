using DebtDungeon.Core;
using System.Collections;
using UnityEngine;

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
        [SerializeField] private Health _health;

        [SerializeField] private float _slowMultiplier = 0.5f;
        [SerializeField] private float _slowDuration = 2f;
        private Coroutine _slowCoroutine;

        [SerializeField] private int _poisonDamage = 1;
        [SerializeField] private float _poisonDuration = 3f;
        private Coroutine _poisonCoroutine;

        // Applies status effect to player
        public void ApplyStatus(StatusEffect statusEffect)
        {
            switch (statusEffect)
            {
                case StatusEffect.Slowed:
                    // Stop an already running slow coroutine
                    if (_slowCoroutine != null)
                    {
                        StopCoroutine(_slowCoroutine);
                    }

                    // Start a new slow coroutine for timer
                    _playerController._terrainMultiplier = _slowMultiplier;
                    _slowCoroutine = StartCoroutine(EffectTimer(_slowDuration, StatusEffect.Slowed));
                    break;
                case StatusEffect.Poisoned:
                    // Stop an already running poison coroutine
                    if (_slowCoroutine != null)
                    {
                        StopCoroutine(_poisonCoroutine);
                    }

                    _poisonCoroutine = StartCoroutine(PoisonDoT());
                    break;
            }
        }

        // Removes status effect
        public void RemoveStatus(StatusEffect statusEffect)
        {
            switch (statusEffect)
            {
                case StatusEffect.Slowed:
                    StopCoroutine(_slowCoroutine);
                    _playerController._terrainMultiplier = 1f;
                    break;
                case StatusEffect.Poisoned:
                    StopCoroutine(_poisonCoroutine);
                    break;
            }
        }

        // Timer to remove status effect
        private IEnumerator EffectTimer(float duration, StatusEffect statusEffect)
        {
            yield return new WaitForSeconds(duration);
            RemoveStatus(statusEffect);
        }

        // Deals poison damage every tick (1 sec)
        private IEnumerator PoisonDoT()
        {
            float startTime = Time.time;
            while (Time.time - startTime <= _poisonDuration)
            {
                yield return new WaitForSeconds(1f); // Poison damages once per sec
                _health.TakeDamage(new DamageInfo(_poisonDamage, DamageType.Poison, null, Faction.Spider));
            }
        }
    }
}
