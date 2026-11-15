using DebtDungeon.Core;
using UnityEngine;

namespace DebtDungeon.AI
{

    public enum UIAnchorPosition
    {
        BottomLeft,
        TopLeft,
        BottomRight,
        TopRight
    }
    /// <summary>
    /// On-screen time-scale controller
    /// Tracks active headcounts per Core Faction, state breakdown, casualties, and gold collected.
    /// </summary>
    public class AIDebugController : MonoBehaviour
    {
        [Header("Overlay Settings")]
        [SerializeField] private bool _showOverlay = true;
        [SerializeField] private float _statsRefreshInterval = 0.25f;

        private int _goblinCount;
        private int _spiderCount;
        private int _wanderCount;
        private int _chaseCount;
        private int _attackCount;
        private int _raidCount;
        private int _totalCasualties;
        private int _totalGoldCollected;
        private float _refreshTimer;

        private void OnEnable()
        {
            GameEvents.ActorKilled += HandleActorKilled;
            GameEvents.GoldCollected += HandleGoldCollected;
        }

        private void OnDisable()
        {
            GameEvents.ActorKilled -= HandleActorKilled;
            GameEvents.GoldCollected -= HandleGoldCollected;
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = _statsRefreshInterval;
                RefreshAgentTelemetry();
            }
        }

        private void HandleActorKilled(KillInfo kill)
        {
            _totalCasualties++;
        }

        private void HandleGoldCollected(int amount)
        {
            _totalGoldCollected += amount;
        }

        private void RefreshAgentTelemetry()
        {
            _goblinCount = 0;
            _spiderCount = 0;
            _wanderCount = 0;
            _chaseCount = 0;
            _attackCount = 0;
            _raidCount = 0;

            FactionAI2D[] agents = FindObjectsByType<FactionAI2D>(FindObjectsSortMode.None);
            for (int i = 0; i < agents.Length; i++)
            {
                FactionAI2D ai = agents[i];
                if (ai == null || !ai.IsAlive)
                {
                    continue;
                }

                if (ai.TryGetComponent(out CombatAgent2D combat))
                {
                    if (combat.Faction == Faction.Goblin)
                    {
                        _goblinCount++;
                    }
                    else if (combat.Faction == Faction.Spider)
                    {
                        _spiderCount++;
                    }
                }

                switch (ai.CurrentState)
                {
                    case AIState.Wander:
                        _wanderCount++;
                        break;
                    case AIState.Chase:
                        _chaseCount++;
                        break;
                    case AIState.Attack:
                        _attackCount++;
                        break;
                    case AIState.Raid:
                        _raidCount++;
                        break;
                }
            }
        }

        private void OnGUI()
        {
            if (!_showOverlay)
            {
                return;
            }

            float panelWidth = 260f;
            float panelHeight = 195f;
            Rect panelRect = new Rect(Screen.width - panelWidth - 15f, 15f, panelWidth, panelHeight);

            GUI.Box(panelRect, "DebtDungeon AI Diagnostics");

            GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 24f, panelWidth - 20f, panelHeight - 30f));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Time Scale: {Time.timeScale:0.0}x");
            if (GUILayout.Button("0.5x", GUILayout.Width(42)))
            {
                Time.timeScale = 0.5f;
            }

            if (GUILayout.Button("1.0x", GUILayout.Width(42)))
            {
                Time.timeScale = 1.0f;
            }

            if (GUILayout.Button("2.0x", GUILayout.Width(42)))
            {
                Time.timeScale = 2.0f;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label($"Goblins: {_goblinCount}  |  Spiders: {_spiderCount}");
            GUILayout.Label($"Wander: {_wanderCount} | Chase: {_chaseCount} | Attack: {_attackCount} | Raid: {_raidCount}");
            GUILayout.Label($"Casualties: {_totalCasualties}  |  Gold Collected: {_totalGoldCollected}");
            GUILayout.Label($"Run Time: {Time.time:0.0}s");

            GUILayout.EndArea();
        }
    }
}
