using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    /// <summary>
    /// Centralized manager listening to agent deaths to instantiate gold drops.
    /// Prevents individual unit prefabs from needing serialized references to the economy coin prefab.
    /// </summary>
    public class GoldDropManager : MonoBehaviour
    {
        private const float DefaultLifetime = 60f;

        [Header("Economy Prefab")]
        [Tooltip("The GoldDrop prefab placed in world space upon actor death.")]
        [SerializeField] private GameObject _goldPrefab;

        public static GoldDropManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Instantiates a gold drop in world space with the specified reward amount.
        /// </summary>
        public void SpawnGold(Vector3 position, int amount)
        {
            if (_goldPrefab == null)
            {
                Debug.LogWarning("[GoldDropManager] Gold prefab is not assigned in the Inspector.", this);
                return;
            }

            GameObject goldObj = Instantiate(_goldPrefab, position, Quaternion.identity);

            GoldDropItem item = goldObj.GetComponent<GoldDropItem>();
            if (item != null)
            {
                item.Initialize(amount, DefaultLifetime);
            }
            else
            {
                Destroy(goldObj, DefaultLifetime);
            }
        }
    }
}
