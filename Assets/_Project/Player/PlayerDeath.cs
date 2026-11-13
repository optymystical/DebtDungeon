using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.Player
{
    public class PlayerDeath : MonoBehaviour
    {
        public void KillPlayer(KillInfo info)
        {
            Destroy(info.Victim);
        }

        private void OnEnable()
        {
            //GameEvents.ActorKilled += KillPlayer;
            GameEvents.PlayerDied += KillPlayer;
        }

        private void OnDisable()
        {
            //GameEvents.ActorKilled -= KillPlayer;
            GameEvents.PlayerDied -= KillPlayer;
        }
    }
}
