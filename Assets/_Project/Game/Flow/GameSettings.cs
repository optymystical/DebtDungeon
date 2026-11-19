using System;
using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// The Alpha options: master, music and sound-effect volume, and fullscreen. Saved in PlayerPrefs so they
    /// survive a restart. Static because settings belong to the whole game, not to one scene or one screen.
    /// </summary>
    public static class GameSettings
    {
        private const string VolumeKey = "DebtDungeon.MasterVolume";
        private const string MusicKey = "DebtDungeon.MusicVolume";
        private const string SfxKey = "DebtDungeon.SfxVolume";
        private const string FullscreenKey = "DebtDungeon.Fullscreen";

        /// <summary>Raised when any volume changes, so music players can update right away (the "preview").</summary>
        public static event Action VolumeChanged;

        /// <summary>Everything, through AudioListener.volume. 0 to 1, in steps of 0.1.</summary>
        public static float MasterVolume { get; private set; } = 1f;

        /// <summary>Music sources multiply by this. 0 to 1, in steps of 0.1.</summary>
        public static float MusicVolume { get; private set; } = 0.6f;

        /// <summary>Sound effects (menu sounds now, combat sounds later) multiply by this. 0 to 1, in steps of 0.1.</summary>
        public static float SfxVolume { get; private set; } = 1f;

        /// <summary>What we asked for. Screen.fullScreen only changes on the next frame, so the menu reads this instead.</summary>
        public static bool Fullscreen { get; private set; } = true;

        // Domain reload is off (Enter Play Mode Options), so clear the static event each time Play starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            VolumeChanged = null;
        }

        // Runs before the first scene's Awake, so music players read the saved volumes when they start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySaved()
        {
            MasterVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.6f);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            AudioListener.volume = MasterVolume;

            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            Screen.fullScreen = Fullscreen;
        }

        /// <summary>Sets the volume of every sound in the game (AudioListener.volume) and saves it.</summary>
        public static void SetMasterVolume(float volume)
        {
            MasterVolume = RoundToStep(volume);
            AudioListener.volume = MasterVolume;
            Save(VolumeKey, MasterVolume);
        }

        /// <summary>Sets the music volume and saves it. Playing music changes at once.</summary>
        public static void SetMusicVolume(float volume)
        {
            MusicVolume = RoundToStep(volume);
            Save(MusicKey, MusicVolume);
        }

        /// <summary>Sets the sound-effect volume and saves it.</summary>
        public static void SetSfxVolume(float volume)
        {
            SfxVolume = RoundToStep(volume);
            Save(SfxKey, SfxVolume);
        }

        /// <summary>Switches fullscreen on or off and saves it. Does nothing visible in the Editor's Game view.</summary>
        public static void SetFullscreen(bool fullscreen)
        {
            Fullscreen = fullscreen;
            Screen.fullScreen = fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        // Round to the nearest 10% so repeated +/- steps don't drift (0.1 isn't exact in a float).
        private static float RoundToStep(float volume)
        {
            return Mathf.Clamp01(Mathf.Round(volume * 10f) / 10f);
        }

        private static void Save(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
            VolumeChanged?.Invoke();
        }
    }
}
