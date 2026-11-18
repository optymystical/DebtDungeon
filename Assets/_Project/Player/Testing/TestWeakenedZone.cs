using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.Player
{
    public class TestWeakenedZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IStatusReceiver target))
            {
                target.ApplyStatus(StatusEffectType.Weakened, 2, 1, gameObject, Faction.Neutral, 1);
            }
        }
    }
}
