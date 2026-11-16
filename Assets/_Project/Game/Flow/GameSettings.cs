using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The two Alpha options, master volume and fullscreen, saved in PlayerPrefs so they survive a restart.
    /// Static because settings belong to the whole game, not to one scene or one screen.
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "DebtDungeon.MasterVolume";
        private const string FullscreenKey = "DebtDungeon.Fullscreen";

        /// <summary>0 to 1, in steps of 0.1.</summary>
        public static float MasterVolume { get; private set; } = 1f;

        /// <summary>What we asked for. Screen.fullScreen only changes on the next frame, so the menu reads this instead.</summary>
        public static bool Fullscreen { get; private set; } = true;

        // Runs once each time Play starts (and once when the built game launches), after the first scene loads.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplySaved()
        {
            MasterVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = MasterVolume;

            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            Screen.fullScreen = Fullscreen;
        }

        /// <summary>Sets the volume of every sound in the game (AudioListener.volume) and saves it.</summary>
        public static void SetMasterVolume(float volume)
        {
            // Round to the nearest 10% so repeated +/- steps don't drift (0.1 isn't exact in a float).
            MasterVolume = Mathf.Clamp01(Mathf.Round(volume * 10f) / 10f);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
            PlayerPrefs.Save();
        }

        /// <summary>Switches fullscreen on or off and saves it. Does nothing visible in the Editor's Game view.</summary>
        public static void SetFullscreen(bool fullscreen)
        {
            Fullscreen = fullscreen;
            Screen.fullScreen = fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
