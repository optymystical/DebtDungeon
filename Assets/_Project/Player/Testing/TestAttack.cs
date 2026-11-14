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
        [SerializeField] private float _lifetime = 1.0f;
        [SerializeField] private float _floatSpeed = 1.5f;
        private List<GameObject> _hitTextObjects;
        private float _elapsed;
        private HitResult _hitResult;

        private void Awake()
        {
            _hitTextObjects = new List<GameObject>();
            _elapsed = 0f;
        }

        private void Update()
        {
            if (_hitTextObjects != null) { 
                foreach (GameObject hitTextObject in _hitTextObjects)
                {
                    // Locks rotation to the camera's
                    hitTextObject.transform.rotation = Camera.main.transform.rotation;

                    // Moves upward
                    hitTextObject.transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

                    // Fades out
                    _elapsed += Time.deltaTime;
                    hitTextObject.GetComponent<TextMeshPro>().alpha = Mathf.Clamp01(1f - (_elapsed / _lifetime));
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

            StartCoroutine(DestroyPrefab(_lifetime, newHitTextObject));
        }

        private IEnumerator DestroyPrefab(float delayTime, GameObject newHitTextObject)
        {
            yield return new WaitForSeconds(delayTime);
            _hitTextObjects.Remove(newHitTextObject);
            Destroy(newHitTextObject);
        }
    }
}
