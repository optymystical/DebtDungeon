using UnityEngine;
using UnityEngine.InputSystem;

namespace DebtDungeon.Player
{
    public class TestStatus : MonoBehaviour
    {
        [SerializeField] private StatusEffects _statusEffects;

        private void Update()
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                _statusEffects.ApplyStatus(StatusEffect.Slowed); // fakes applying status
            }
            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                _statusEffects.RemoveStatus(StatusEffect.Slowed); // fakes removing status
            }
        }
    }
}
