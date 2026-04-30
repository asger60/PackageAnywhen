using Anywhen.Composing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Anywhen
{
    public class AnywhenPlaybackController : MonoBehaviour
    {
        AnysongObject _currentSong;
        [SerializeField] private AnysongObject[] midis;
        [SerializeField] private AnysongObject[] sounds;
        static AnywhenAudioGenerator _currentPlayer;

        [SerializeField] [Range(0, 1f)] private float testIntensity;
        [SerializeField] [Range(0, 1f)] private float testSnapshot;


        void Start()
        {
            _currentSong = midis[0];
            var songSource = gameObject.AddComponent<AudioSource>();
            _currentPlayer = ScriptableObject.CreateInstance<AnywhenAudioGenerator>();
            _currentPlayer.SetSong(_currentSong);
            songSource.generator = _currentPlayer;
            songSource.Play();
            _currentPlayer.SetPlay(true);
            AnywhenRuntime.SetTempo(100);
        }

        private void Update()
        {
            _currentPlayer.SetIntensity(testIntensity);
            _currentPlayer.SetSnapshot(testSnapshot);


            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                OnSoundPressed(0);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                OnSoundPressed(1);
            }

            if (Keyboard.current.digit8Key.wasPressedThisFrame)
            {
                OnMidiButtonPressed(0);
            }

            if (Keyboard.current.digit9Key.wasPressedThisFrame)
            {
                OnMidiButtonPressed(1);
            }
        }

        private void OnSoundPressed(int index)
        {
            Debug.Log($"Button 1 pressed: {index}");
            _currentPlayer.Load(sounds[index], AnywhenAudioGenerator.LoadOptions.OnlyTrackSounds);
        }

        void OnMidiButtonPressed(int index)
        {
            _currentPlayer.Load(midis[index], AnywhenAudioGenerator.LoadOptions.OnlyMidiSettings);
        }


        private void OnDestroy()
        {
            AnywhenSnapshotBlender.ApplyBlend(_currentSong, 0);
            _currentSong.RefreshSettings();
            _currentSong.RemoveListeners();
        }
    }
}