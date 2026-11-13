using UnityEngine;
using UnityEngine.InputSystem;

namespace DebtDungeon.Player
{
    public class TestBurden : MonoBehaviour
    {
        [SerializeField] private PlayerController _targetScript;

        private void Update()
        {
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                _targetScript.SetBurdenSteps(0); // fakes Progression
            }
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                _targetScript.SetBurdenSteps(1); // fakes Progression
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                _targetScript.SetBurdenSteps(2); // fakes Progression
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                _targetScript.SetBurdenSteps(3); // to test cap
            }
        }
    }
}
