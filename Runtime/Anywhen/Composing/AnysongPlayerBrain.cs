using System;
using UnityEngine;

namespace Anywhen.Composing
{
    public class AnysongPlayerBrain : MonoBehaviour
    {
        AnywhenPlayer _currentPlayer;


        private AnywhenPlayer _nextUpPlayer, _stopPlayer;
        [Range(0, 1f)] [SerializeField] private float globalIntensity;


        public static int SectionLockIndex;
        public static Action<float> OnIntensityChanged;

        public enum TransitionTypes
        {
            Instant,
            NextBar,
            CrossFade
        }




        public static float GlobalIntensity => AnywhenRuntime.AnysongPlayerBrain.globalIntensity;
        private bool _isStarted;
        public static bool IsStarted => AnywhenRuntime.AnysongPlayerBrain._isStarted;

        private void Awake()
        {
            _currentPlayer = null;
        }

        private void Start()
        {
            SetGlobalIntensity(1);
            SetSectionLock(-1);
            AnywhenRuntime.Metronome.OnNextBar += OnNextBar;
        }


        private void OnNextBar()
        {
            if (_nextUpPlayer != null)
            {
                TransitionNow(_nextUpPlayer);
                _nextUpPlayer = null;
            }

            if (_stopPlayer != null)
            {
                TransitionNow(null);
                _stopPlayer = null;
            }
        }

        public static void TransitionTo(AnywhenPlayer player, TransitionTypes transitionType) =>
            AnywhenRuntime.AnysongPlayerBrain.HandleTransitionToPlayer(player, transitionType);


        public static void SetGlobalIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            AnywhenRuntime.AnysongPlayerBrain.globalIntensity = intensity;
            OnIntensityChanged?.Invoke(AnywhenRuntime.AnysongPlayerBrain.globalIntensity);
        }

        public static void ModifyGlobalIntensity(float amount)
        {
            AnywhenRuntime.AnysongPlayerBrain.globalIntensity += amount;
            AnywhenRuntime.AnysongPlayerBrain.globalIntensity = Mathf.Clamp01(AnywhenRuntime.AnysongPlayerBrain.globalIntensity);
            OnIntensityChanged?.Invoke(AnywhenRuntime.AnysongPlayerBrain.globalIntensity);
        }


        private void HandleTransitionToPlayer(AnywhenPlayer player, TransitionTypes transitionType)
        {
            _isStarted = true;
            if (AnywhenRuntime.IsPreviewing)
            {
                TransitionNow(player);
                return;
            }


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
                    _nextUpPlayer.AttachToMetronome();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleTransitionToNothing(AnywhenPlayer player, TransitionTypes transitionType)
        {
            switch (transitionType)
            {
                case TransitionTypes.Instant:
                    TransitionNow(null);
                    break;
                case TransitionTypes.NextBar:
                    _stopPlayer = player;
                    break;
                case TransitionTypes.CrossFade:
                    Debug.LogWarning("stopping doesn't support crossfade");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void TransitionNow(AnywhenPlayer newPlayer)
        {
            if (_currentPlayer != null)
            {
                _currentPlayer.ReleaseFromMetronome();
            }

            if (newPlayer != null)
            {
                _currentPlayer = newPlayer;
                _currentPlayer.AttachToMetronome();
            }
            else
            {
                _currentPlayer?.ReleaseFromMetronome();
                _currentPlayer = null;
            }
        }

        public static void SetSectionLock(int index)
        {
            SectionLockIndex = index;
        }

        public static AnywhenPlayer GetCurrentPlayer()
        {
            return AnywhenRuntime.AnysongPlayerBrain._currentPlayer;
        }

        public static void StopPlayer(AnywhenPlayer player, TransitionTypes transitionType)
        {
            AnywhenRuntime.AnysongPlayerBrain.HandleTransitionToNothing(player, transitionType);
        }
    }
}