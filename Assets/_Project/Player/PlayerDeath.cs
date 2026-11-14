using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.Player
{
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private Health _eventRaiser;
        public void KillPlayer(KillInfo info)
        {
            info.Victim.SetActive(false);
            info.Victim.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }

        private void OnEnable()
        {
            _eventRaiser.Died += KillPlayer;
        }

        private void OnDisable()
        {
            _eventRaiser.Died -= KillPlayer;
        }
    }
}
