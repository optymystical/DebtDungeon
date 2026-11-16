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

    }
}
