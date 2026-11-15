using UnityEngine;
using DebtDungeon.Core;

namespace DebtDungeon.AI
{
    
    /// <summary>
    /// Event-driven economy manager that listens to Core's GameEvents.ActorKilled
    /// and spawns timed gold drops at kill.Position without singletons.
    /// </summary>
    public class GoldDropManager : MonoBehaviour
    {
        private const float DefaultLifetime = 60f;

        [Header("Economy Prefab")]
        [Tooltip("The GoldDrop prefab placed in world space upon actor death.")]
        [SerializeField] private GameObject _goldPrefab;

        private void OnEnable()
        {
            GameEvents.ActorKilled += HandleActorKilled;
        }

        private void OnDisable()
        {
            GameEvents.ActorKilled -= HandleActorKilled;
        }

        private void HandleActorKilled(KillInfo kill)
        {
            if (kill.GoldReward <= 0)
            {
                return;
            }

            if (_goldPrefab == null)
            {
                Debug.LogWarning("[GoldDropManager] Gold prefab is not assigned in the Inspector.", this);
                return;
            }

            GameObject goldObj = Instantiate(_goldPrefab, kill.Position, Quaternion.identity);

            GoldDropItem item = goldObj.GetComponent<GoldDropItem>();
            if (item != null)
            {
                item.Initialize(kill.GoldReward, DefaultLifetime);
            }
            else
            {
                Destroy(goldObj, DefaultLifetime);
            }
        }
    }
}
