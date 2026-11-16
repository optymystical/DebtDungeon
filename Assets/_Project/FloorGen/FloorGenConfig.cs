using UnityEngine;

namespace DebtDungeon.FloorGen
{

    /// <summary>
    /// Tuning values for floor generation. One share asset, so the sandbox and Main scenes build the same way
    /// </summary>
    [CreateAssetMenu(fileName = "FloorGenConfig", menuName = "DebtDungeon/Floor Gen/Floor Gen Config")]
    public class FloorGenConfig : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Chunks per side. 5 = a 5x5 floor.")]
        [SerializeField, Min(3)] private int _gridSize = 5;

        [Header("Hotspots")]
        [Tooltip("Minimum distance, in chunk cells, from the start to each hotsppot and between the two hotspots")]
        [SerializeField, Min(1)] private int _minHotspotDistance = 2;
        [Tooltip("Weakest hotspot strength a seed can roll.")]
        [SerializeField, Min(0f)] private float _minHotspotStrength = 0.8f;
        [Tooltip("Strongest hotspot strength a seed can roll.")]
        [SerializeField, Min(0f)] private float _maxHotspotStrength = 1.2f;
        [Tooltip("Distance, in chunk cells, where a hotspot's influence fades to zero.")]
        [SerializeField, Min(0.1f)] private float _heatRadius = 3f;

        [Header("Intensity")]
        [Tooltip("Heat needed for a faction to claim a cell as Minor territory.")]
        [SerializeField, Range(0f, 1f)] private float _minorThreshold = 0.35f;
        [Tooltip("Heat needed for a Major stronghold.")]
        [SerializeField, Range(0f, 1.5f)] private float _majorThreshold = 0.75f;
        [SerializeField, Min(1)] private int _maxMajorPerFaction = 2;

        [Header("Connections")]
        [Tooltip("Extra links added after the spanning tree so floors have loops. 6 tested best in the Week 2 evidence test.")]
        [SerializeField, Min(0)] private int _extraConnections = 6;

        public int GridSize => _gridSize;
        public int MinHotspotDistance => _minHotspotDistance;
        public float MinHotspotStrength => _minHotspotStrength;
        public float MaxHotspotStrength => _maxHotspotStrength;
        public float HeatRadius => _heatRadius;
        public float MinorThreshold => _minorThreshold;
        public float MajorThreshold => _majorThreshold;
        public int MaxMajorPerFaction => _maxMajorPerFaction;
        public int ExtraConnections => _extraConnections;

        private void OnValidate()
        {
            // keeping the ranges sensible when a team mate drags one value past the other
            if (_maxHotspotStrength < _minHotspotStrength)
            {
                _maxHotspotStrength = _minHotspotStrength;
            }

            if (_majorThreshold < _minorThreshold)
            {
                _majorThreshold = _minorThreshold;
            }
        }
    }
}
