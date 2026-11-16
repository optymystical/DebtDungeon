using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    [RequireComponent(typeof(Collider2D))]
    public class ExitKeyPickup2D : MonoBehaviour
    {
        [SerializeField] private string _pickupMessage = "Exit Key Acquired!";

        private bool _collected;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected)
            {
                return;
            }

            Health h = other.GetComponentInParent<Health>();
            if (h != null && h.Faction == Faction.Player)
            {
                _collected = true;
                Debug.Log($"[ExitKey] {_pickupMessage}");
                // Hook into your Player inventory and Floor exit gate here
                Destroy(gameObject);
            }
        }
    }
}
