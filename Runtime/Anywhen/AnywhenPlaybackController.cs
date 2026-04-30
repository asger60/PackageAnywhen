using System;
using Anywhen.Composing;
using UnityEngine;

namespace Anywhen
{
    public class AnywhenPlaybackController : MonoBehaviour
    {
        [SerializeField] AnysongObject song;
        static AnywhenAudioGenerator _currentPlayer;

        [SerializeField] [Range(0, 1f)] private float testIntensity;
        [SerializeField] [Range(0, 1f)] private float testSnapshot;
        
        void Start()
        {
            var songSource = gameObject.AddComponent<AudioSource>();
            _currentPlayer = ScriptableObject.CreateInstance<AnywhenAudioGenerator>();
            _currentPlayer.SetSong(song);
            songSource.generator = _currentPlayer;
            songSource.Play();
            _currentPlayer.SetPlay(true);
            AnywhenRuntime.SetTempo(100);
        }

        private void Update()
        {
            _currentPlayer.SetIntensity(testIntensity);
            AnywhenSnapshotBlender.ApplyBlend(song, testSnapshot);
            song.RefreshSettings();
        }

        private void OnDestroy()
        {
            AnywhenSnapshotBlender.ApplyBlend(song, 0);
            song.RefreshSettings();
        }
    }
}