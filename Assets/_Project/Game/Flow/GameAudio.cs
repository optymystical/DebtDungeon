using UnityEngine;

namespace DebtDungeon.Game
{
    /// <summary>
    /// One per scene, on the GameManager's object: loops this scene's music track at the music volume and
    /// hands the menu sounds to UISounds. MainMenu gets the menu track; Main gets the dungeon track.
    /// Moving between menus inside one scene (title, options, creator) never restarts the music,
    /// because they're all screens in the same scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameAudio : MonoBehaviour
    {
        [Tooltip("Loops while this scene is open. Leave empty for silence.")]
        [SerializeField] private AudioClip _music;

        [Tooltip("Plays when the selection moves to another button.")]
        [SerializeField] private AudioClip _uiMove;

        [Tooltip("Plays when a button is pressed.")]
        [SerializeField] private AudioClip _uiClick;

        private AudioSource _musicSource;

        private void Awake()
        {
            UISounds.SetClips(_uiMove, _uiClick);

            if (_music == null)
            {
                return;
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.clip = _music;
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            ApplyVolume();
            _musicSource.Play();
        }

        private void OnEnable()
        {
            GameSettings.VolumeChanged += ApplyVolume;
        }

        private void OnDisable()
        {
            GameSettings.VolumeChanged -= ApplyVolume;
        }

        private void ApplyVolume()
        {
            if (_musicSource != null)
            {
                // AudioSource.volume is linear: 0.5 really is half the signal, which is what the card asks for.
                // (An Audio Mixer works in decibels instead, where "half the slider" isn't half the volume.)
                _musicSource.volume = GameSettings.MusicVolume;
            }
        }
    }
}
