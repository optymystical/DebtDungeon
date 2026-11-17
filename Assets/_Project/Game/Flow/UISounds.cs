using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// Plays the menu sounds (move and click) at the SFX volume. Uses one AudioSource on a hidden object kept
    /// with DontDestroyOnLoad, so a click that loads a scene (Start Run, Retry, Main Menu) isn't cut off when
    /// the old scene unloads. The clips come from the scene's GameAudio component.
    /// </summary>
    public static class UISounds
    {
        private static AudioClip _moveClip;
        private static AudioClip _clickClip;
        private static AudioSource _source;

        // Domain reload is off, so forget the last Play session's clips and (destroyed) source.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _moveClip = null;
            _clickClip = null;
            _source = null;
        }

        /// <summary>Called by GameAudio in each scene. A null clip keeps the one already set.</summary>
        public static void SetClips(AudioClip moveClip, AudioClip clickClip)
        {
            if (moveClip != null)
            {
                _moveClip = moveClip;
            }

            if (clickClip != null)
            {
                _clickClip = clickClip;
            }
        }

        /// <summary>Selection moved to another button (arrow keys or mouse hover).</summary>
        public static void PlayMove()
        {
            Play(_moveClip);
        }

        /// <summary>A button was pressed.</summary>
        public static void PlayClick()
        {
            Play(_clickClip);
        }

        private static void Play(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            // Unity's == null is also true for a destroyed object, e.g. after Play mode stopped.
            if (_source == null)
            {
                GameObject soundObject = new GameObject("UI Sounds");
                Object.DontDestroyOnLoad(soundObject);
                _source = soundObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }

            // PlayOneShot lets quick presses overlap instead of cutting each other off.
            _source.PlayOneShot(clip, GameSettings.SfxVolume);
        }
    }
}
