using UnityEngine;
using System;

namespace DebtDungeon.AI
{
    public class NoiseEventBus
    {
        /// <summary>Parameters: (worldPosition, maxAudibleRadius)</summary>
        public static event Action<Vector2, float> OnNoiseEmitted;

        public static void EmitNoise(Vector2 worldPosition, float soundRadius = 8f)
        {
            OnNoiseEmitted?.Invoke(worldPosition, soundRadius);
        }

        // The project has domain reload turned off (Enter Play Mode Options), so static events keep their
        // listeners between Play sessions. Clear them when Play starts, the same way GameEvents does.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            OnNoiseEmitted = null;
        }
    }
}
