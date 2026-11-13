using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Sits on an instantiated gold coin or pile.
    /// Handles the 60-second despawn timer and player trigger collection.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class GoldDropItem : MonoBehaviour
    {
        private const float DefaultLifetimeSeconds = 60f;

        [Header("Drop Settings")]
        [SerializeField, Min(1)] private int _goldAmount = 5;
        [SerializeField] private float _lifetimeSeconds = DefaultLifetimeSeconds;

        private float _despawnTimer;
        private bool _collected;

        public int GoldAmount => _goldAmount;

        public void Initialize(int amount, float lifetime = DefaultLifetimeSeconds)
        {
            _goldAmount = Mathf.Max(1, amount);
            _lifetimeSeconds = lifetime;
            _despawnTimer = _lifetimeSeconds;
        }

        private void Awake()
        {
            _despawnTimer = _lifetimeSeconds;
            // Enforce trigger collider
            CircleCollider2D col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;
        }

        private void Update()
        {
            _despawnTimer -= Time.deltaTime;
            if (_despawnTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected)
            {
                return;
            }

            // Layer 6: Player or player component check
            if (other.gameObject.layer == 6 || other.CompareTag("Player") || other.GetComponent<Player2D>() != null)
            {
                // In sandbox: log reward. When Core economy merges: add to Player Inventory/Bank
                Debug.Log($"[Economy] Player collected {_goldAmount} gold!");
                _collected = true;
                GameEvents.RaiseGoldCollected(_goldAmount);
                Destroy(gameObject);
            }
        }
    }
}
