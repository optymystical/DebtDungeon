using UnityEngine;
using UnityEngine.InputSystem;

namespace DebtDungeon.Player
{
    public class TestBurden : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;

        private void Update()
        {
            if (Keyboard.current.digit0Key.wasPressedThisFrame)
            {
                _player.SetBurdenSteps(0); // fakes Progression
            }
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                _player.SetBurdenSteps(1); // fakes Progression
            }
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                _player.SetBurdenSteps(2); // fakes Progression
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                _player.SetBurdenSteps(3); // to test cap
            }
        }
    }
}
