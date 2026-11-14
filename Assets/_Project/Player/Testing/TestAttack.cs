using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using System.Collections.Generic;

namespace DebtDungeon.Player
{
    public class TestAttack : MonoBehaviour
    {
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private GameObject _HitTextPrefab;
        [SerializeField] private int _accuracy = 100;
        [SerializeField] private int _baseDamage = 10;
        private List<GameObject> _hitTextObjects;
        private HitResult _hitResult;

        private void Awake()
        {
            _hitTextObjects = new List<GameObject>();
        }

        private void Update()
        {
            // Locks any hit text objects rotation to the camera's

            if (_hitTextObjects != null) { 
                foreach (GameObject hitTextObject in _hitTextObjects)
                {
                    hitTextObject.transform.rotation = Camera.main.transform.rotation;
                }
            }

            // Fakes Attack
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                RollToHit();
            }
        }

        private void RollToHit()
        {
            // Instantiate prefab as a child of player
            GameObject newHitTextObject = Instantiate(_HitTextPrefab, _playerTransform);
            _hitTextObjects.Add(newHitTextObject);
            TextMeshPro HitText = newHitTextObject.GetComponent<TextMeshPro>();

            _hitResult = CombatRules.RollHit(_accuracy);

            switch (_hitResult)
            {
                case HitResult.Critical:
                    HitText.text = "Critical ";
                    HitText.color = Color.yellow;
                    break;
                case HitResult.Hit:
                    HitText.text = "Hit ";
                    HitText.color = Color.white;
                    break;
                case HitResult.Miss:
                    HitText.text = "Miss";
                    HitText.color = Color.red;
                    break;
            }

            if (_hitResult != HitResult.Miss)
            {
                HitText.text += CombatRules.GetDamage(_baseDamage, _hitResult).ToString();
            }

            // Fade out to 0 alpha over 1 second
            HitText.CrossFadeAlpha(0, 1f, true);

            StartCoroutine(DestroyPrefab(1f, newHitTextObject));
        }

        private IEnumerator DestroyPrefab(float delayTime, GameObject newHitTextObject)
        {
            yield return new WaitForSeconds(delayTime);
            _hitTextObjects.Remove(newHitTextObject);
            Destroy(newHitTextObject);
        }
    }
}
