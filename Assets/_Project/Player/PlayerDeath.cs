using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.Player
{
    public class PlayerDeath : MonoBehaviour
    {
        [SerializeField] private Health _Health;
        public void KillPlayer(KillInfo info)
        {
            info.Victim.GetComponent<PlayerController>().enabled = false;
            info.Victim.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            info.Victim.GetComponent<Rigidbody2D>().simulated = false;
            info.Victim.GetComponent<PolygonCollider2D>().enabled = false;
            info.Victim.GetComponent<SpriteRenderer>().color = Color.gray;
        }

        private void OnEnable()
        {
            _Health.Died += KillPlayer;
        }

        private void OnDisable()
        {
            _Health.Died -= KillPlayer;
        }
    }
}
