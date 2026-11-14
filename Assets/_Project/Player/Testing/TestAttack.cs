using DebtDungeon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

namespace DebtDungeon.Player
{
    public class TestAttack : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private GameObject _HitTextObject;
        [SerializeField] private TextMeshPro _HitText;
        [SerializeField] private int _accuracy = 100;

        private HitResult _hitResult;

        private void Update()
        {
            _HitText.transform.rotation = Camera.main.transform.rotation;

            if (Keyboard.current.spaceKey.wasPressedThisFrame) // fakes Attack
            {
                _HitTextObject.SetActive(true);
                _hitResult = CombatRules.RollHit(_accuracy); 

                switch (_hitResult)
                {
                    case HitResult.Critical:
                        _HitText.text = "Critical";
                        _HitText.color = Color.yellow;
                        break;
                    case HitResult.Hit:
                        _HitText.text = "Hit";
                        _HitText.color = Color.white;
                        break;
                    case HitResult.Miss:
                        _HitText.text = "Miss";
                        _HitText.color = Color.red;
                        break;
                }

                StartCoroutine(SetInactive(0.5f));
            }
        }
        private IEnumerator SetInactive(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            _HitTextObject.SetActive(false);
        }
    }
}
