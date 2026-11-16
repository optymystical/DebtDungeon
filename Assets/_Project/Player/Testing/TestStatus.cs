using DebtDungeon.Core;
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
                _statusEffects.ApplyStatus(StatusEffectType.Slowed); // fakes applying slowed
            }
            else if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                _statusEffects.RemoveStatus(StatusEffectType.Slowed); // fakes removing slowed
            }
            else if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                _statusEffects.ApplyStatus(StatusEffectType.Poisoned); // fakes applying poisoned
            }
            else if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                _statusEffects.RemoveStatus(StatusEffectType.Poisoned); // fakes removing poisoned
            }
        }
    }
}
