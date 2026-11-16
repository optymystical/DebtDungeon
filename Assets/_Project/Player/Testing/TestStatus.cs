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
                _statusEffects.ApplyStatus(StatusEffect.Slowed); // fakes applying slowed
            }
            else if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                _statusEffects.RemoveStatus(StatusEffect.Slowed); // fakes removing slowed
            }
            else if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                _statusEffects.ApplyStatus(StatusEffect.Poisoned); // fakes applying poisoned
            }
            else if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                _statusEffects.RemoveStatus(StatusEffect.Poisoned); // fakes removing poisoned
            }
        }
    }
}
