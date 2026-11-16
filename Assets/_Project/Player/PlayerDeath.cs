using UnityEngine;
using UnityEngine.Serialization;
using DebtDungeon.Core;

namespace DebtDungeon.Player
{
    public class PlayerDeath : MonoBehaviour
    {
        [FormerlySerializedAs("_Health")]
        [SerializeField] private Health _health;
        private void Awake()
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }

        public void KillPlayer(KillInfo info)
        {
            info.Victim.GetComponent<PlayerController>().enabled = false;
            info.Victim.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            info.Victim.GetComponent<Rigidbody2D>().simulated = false;
            info.Victim.GetComponent<Collider2D>().enabled = false;
            info.Victim.GetComponent<SpriteRenderer>().color = Color.gray;
        }

        private void OnEnable()
        {
            _health.Died += KillPlayer;
        }

        private void OnDisable()
        {
            _health.Died -= KillPlayer;
        }
    }
}
