using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Anywhen.Composing
{
    public class AnysongPlayerBrain : MonoBehaviour
    {
        [SerializeField] private AnysongPlayer[] songPlayers;

        AnysongPlayer _currentPlayer;

        private AnysongPlayer _nextUpPlayer;
        [Range(0, 1f)] [SerializeField] private float globalIntensity;

        public enum TransitionTypes
        {
            Instant,
            NextBar,
            CrossFade,
            TrackEnd
        }

        public TransitionTypes transitionType;
        private static AnysongPlayerBrain _instance;


        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            LateInit();
            AnywhenRuntime.Metronome.OnNextBar += OnNextBar;
        }


        async void LateInit()
        {
            foreach (var player in songPlayers)
            {
                if (!player.IsSongLoaded)
                    await Task.Yield();
            }

            _currentPlayer = songPlayers[0];
            _currentPlayer.Play();
        }

        private void Update()
        {
            foreach (var anysongPlayer in songPlayers)
            {
                anysongPlayer.SetGlobalIntensity(globalIntensity);
            }
        }


        private void OnNextBar()
        {
            if (_nextUpPlayer != null)
            {
                if (transitionType == TransitionTypes.TrackEnd)
                {
                    if (_currentPlayer.GetTrackProgress() == 0f)
                    {
                        TransitionNow(_nextUpPlayer);
                        _nextUpPlayer = null;
                    }
                }
                else
                {
                    TransitionNow(_nextUpPlayer);
                    _nextUpPlayer = null;
                }
            }
        }

        public static void TransitionTo(AnysongPlayer player)
        {
            _instance.StartToPlayer(player);
        }


        public static void SetGlobalIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            _instance.globalIntensity = intensity;
        }


        private void StartToPlayer(AnysongPlayer player)
        {
            switch (transitionType)
            {
                case TransitionTypes.Instant:
                    TransitionNow(player);
                    break;
                case TransitionTypes.NextBar:
                    _nextUpPlayer = player;
                    break;
                case TransitionTypes.CrossFade:
                    _nextUpPlayer = player;
                    _nextUpPlayer.Play();
                    break;
                case TransitionTypes.TrackEnd:
                    _nextUpPlayer = player;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void TransitionNow(AnysongPlayer newPlayer)
        {
            if (_currentPlayer != null)
            {
                _currentPlayer.SetMixIntensity(0);
                _currentPlayer.Stop();
            }

            _currentPlayer = newPlayer;
            _currentPlayer.Play();
        }
    }
}