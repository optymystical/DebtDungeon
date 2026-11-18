using DebtDungeon.FloorGen;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Builds the Main scene's floor with this run's seed. Every load of Main is a new run (Retry included), so it
    /// rolls a fresh seed, unless the Inspector override is on, which rebuilds one exact floor for a bug report.
    /// </summary>
    [DisallowMultipleComponent]
    public class FloorBootstrap : MonoBehaviour
    {
        [SerializeField] private FloorGenerator _floorGenerator;

        [Header("Debug")]
        [Tooltip("Build this exact floor instead of rolling a new seed. Copy the seed from the 'Floor generated' log.")]
        [SerializeField] private bool _useSeedOverride;
        [SerializeField] private int _seedOverride;

        // Start, not Awake: listeners like AI's FactionFloorDirector subscribe in OnEnable, and every OnEnable
        // in the scene runs before any Start, so nobody misses FloorGenerated
        private void Start()
        {
            if (_floorGenerator == null)
            {
                Debug.LogError("FloorBootstrap needs the scene's FloorGenerator.", this);
                return;
            }

            if (_useSeedOverride)
            {
                RunSession.SetSeed(_seedOverride);
            }
            else
            {
                RunSession.RollNewSeed();
            }

            _floorGenerator.Generate(RunSession.Seed);
        }
    }
}
