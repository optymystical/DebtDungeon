using System;
using DebtDungeon.Progression;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Carries the creator's choices from the MainMenu scene into the Main scene. It holds the blueprint, not a
    /// character: the Main scene builds a fresh level-1 character from it every time it loads, so Retry is just
    /// "load Main again". Static so it survives scene loads without a DontDestroyOnLoad object, the same way
    /// GameEvents does. Gregory's run seed can live here too once his card lands.
    /// </summary>
    public static class RunSession
    {
        /// <summary>The creator's choices for this run, or null before the first Confirm.</summary>
        public static CharacterBlueprint Blueprint { get; private set; }

        /// <summary>True once the creator has confirmed a character.</summary>
        public static bool HasBlueprint => Blueprint != null;

        /// <summary>Stores the creator's choices. Called by the creator screen just before GameManager.StartRun.</summary>
        public static void SetBlueprint(CharacterBlueprint blueprint)
        {
            if (blueprint == null)
            {
                throw new ArgumentNullException(nameof(blueprint));
            }

            Blueprint = blueprint;
        }

        /// <summary>
        /// Builds a fresh level-1 character for a run (first try or Retry). Returns null if nothing was confirmed,
        /// e.g. the Main scene was opened directly; the bootstrap then falls back to a default preset.
        /// </summary>
        public static CharacterData CreateCharacterForRun()
        {
            return HasBlueprint ? Blueprint.Build() : null;
        }

        /// <summary>Forgets the blueprint.</summary>
        public static void Clear()
        {
            Blueprint = null;
        }

        /// <summary>Forgets the last session's blueprint when Play mode starts (static data outlives scenes).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Blueprint = null;
        }
    }
}
