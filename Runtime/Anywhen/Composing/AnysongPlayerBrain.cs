using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Anywhen.Composing
{
    public class AnysongPlayerBrain : MonoBehaviour
    {
        AnysongPlayer _currentPlayer;

        private AnysongPlayer _nextUpPlayer;
        [Range(0, 1f)] [SerializeField] private float globalIntensity;


        public static int SectionLockIndex;
        public static Action<float> OnIntensityChanged;

        public enum TransitionTypes
        {
            Instant,
            NextBar,
            CrossFade,
            TrackEnd
        }

        private TransitionTypes _nextTransitionType;

        private static AnysongPlayerBrain _instance;

        private static AnysongPlayerBrain Instance
        {
            get
            {
                if (_instance)
                    return _instance;
                _instance = FindObjectOfType<AnysongPlayerBrain>();
                return _instance;
            }
        }

        public static float GlobalIntensity => Instance.globalIntensity;


        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            SetGlobalIntensity(1);
            SectionLockIndex = 0;
            AnywhenRuntime.Metronome.OnNextBar += OnNextBar;
        }


        private void OnNextBar()
        {
            if (_nextUpPlayer != null)
            {
                if (_nextTransitionType == TransitionTypes.TrackEnd)
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

        public static void TransitionTo(AnysongPlayer player, TransitionTypes transitionType) =>
            Instance.HandleTransitionToPlayer(player, transitionType);


        public static void SetGlobalIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            Instance.globalIntensity = intensity;
            OnIntensityChanged?.Invoke(Instance.globalIntensity);
        }

        public static void ModifyGlobalIntensity(float amount)
        {
            Instance.globalIntensity += amount;
            Instance.globalIntensity = Mathf.Clamp01(Instance.globalIntensity);
            OnIntensityChanged?.Invoke(Instance.globalIntensity);
        }


        private void HandleTransitionToPlayer(AnysongPlayer player, TransitionTypes transitionType)
        {
            Debug.Log("transition " + transitionType);
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
                case TransitionTypes.TrackEnd:
                    _nextUpPlayer = player;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _nextTransitionType = transitionType;
        }

        void TransitionNow(AnysongPlayer newPlayer)
        {
            if (_currentPlayer != null)
            {
                _currentPlayer.SetMixIntensity(0);
                _currentPlayer.ReleaseFromMetronome();
            }

            _currentPlayer = newPlayer;
            _currentPlayer.AttachToMetronome();
        }

        public static void SetSectionLock(int index)
        {
            SectionLockIndex = index;
        }
    }
}