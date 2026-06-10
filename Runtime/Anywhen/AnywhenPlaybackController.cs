using System.Collections;
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
        AnywhenAudioGenerator _currentPlayer;

        [SerializeField] [Range(0, 1f)] private float testIntensity;
        [SerializeField] [Range(0, 1f)] private float testSnapshot;

        [AnywhenTrackType] [SerializeField] private int overrideTrackTypeIndex;

        IEnumerator Start()
        {
            _currentSong = midis[0];
            var songSource = gameObject.AddComponent<AudioSource>();
            _currentPlayer = ScriptableObject.CreateInstance<AnywhenAudioGenerator>();
            _currentPlayer.SetSong(_currentSong);
            songSource.generator = _currentPlayer;
            songSource.Play();
            yield return new WaitForSeconds(0.1f);
            AnywhenRuntime.SetTempo(100);
            AnywhenRuntime.Reset();
            AnywhenRuntime.Metronome.Play();
            _currentPlayer.SetPlay(true, 0, false);
            _currentPlayer.OnMidiEventTriggered += OnMidiEventTriggered;
        }

        private void OnMidiEventTriggered(AnywhenAudioGenerator.MidiDataEvent[] midiDataEvents)
        {
            foreach (var midiDataEvent in midiDataEvents)
            {
                if (midiDataEvent.IsNull()) continue;
                Debug.Log("playing note: " + midiDataEvent.MidiNote + " " + midiDataEvent.TrackTypeIndex);
            }
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
            _currentPlayer.OverrideTrackSettings(sounds[index], overrideTrackTypeIndex);
        }

        void OnMidiButtonPressed(int index)
        {
            _currentPlayer.Load(midis[index], AnywhenAudioGenerator.LoadOptions.OnlyMidiSettings);
        }


        private void OnDestroy()
        {
            AnywhenSnapshotBlender.ApplyBlend(_currentSong, 0);
        }
    }
}