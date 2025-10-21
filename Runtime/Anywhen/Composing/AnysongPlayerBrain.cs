using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anywhen.Composing
{
    public class AnysongPlayerBrain : MonoBehaviour
    {
        private AnywhenPlayer _stopPlayer;
        [Range(0, 1f)] [SerializeField] private float globalIntensity;


        public static Action<float> OnIntensityChanged;

        public enum TransitionTypes
        {
            Instant,
            NextBar,
            CrossFade
        }

        public enum TransitionMode
        {
            OvertakeInstant,
            OvertakeNextBar,
            MixInstant,
            MixNextBar
        }

        public enum TriggerBehaviour
        {
            StartPlayer,
            StopPlayer
        }

        public static float GlobalIntensity => AnywhenRuntime.AnysongPlayerBrain.globalIntensity;
        private bool _isStarted;
        public static bool IsStarted => AnywhenRuntime.AnysongPlayerBrain._isStarted;
        private List<AnywhenPlayer> _players = new List<AnywhenPlayer>();


        private class NextUpPlayer
        {
            public AnywhenPlayer Player;
            public TransitionMode TransitionMode;
            public TriggerBehaviour TriggerBehaviour;

            public NextUpPlayer(AnywhenPlayer player, TransitionMode transitionMode, TriggerBehaviour triggerBehaviour)
            {
                Player = player;
                TransitionMode = transitionMode;
                TriggerBehaviour = triggerBehaviour;
            }
        }

        private NextUpPlayer _nextPlayer;

        private void Start()
        {
            AnywhenRuntime.Metronome.OnNextBar += OnNextBar;
        }


        private void OnNextBar()
        {
            if (_nextPlayer != null)
            {
                if (_nextPlayer.TriggerBehaviour == TriggerBehaviour.StartPlayer)
                {
                    if (_nextPlayer.TransitionMode == TransitionMode.OvertakeNextBar)
                    {
                        StopAllPlayers();
                    }
                    _nextPlayer.Player.Play();
                }

                if (_nextPlayer.TriggerBehaviour == TriggerBehaviour.StopPlayer)
                {
                    _nextPlayer.Player.Stop();
                }                

                _nextPlayer = null;
            }
        }


        public static void TransitionTo(AnywhenPlayer player, TriggerBehaviour triggerBehaviour, TransitionMode transitionType) =>
            AnywhenRuntime.AnysongPlayerBrain.HandleTransitionToPlayer(player, triggerBehaviour, transitionType);


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


        void StopAllPlayers()
        {
            for (var i = _players.Count - 1; i >= 0; i--)
            {
                var player = _players[i];
                player.Stop();
            }

            _players.Clear();
        }

        public static void RegisterPlay(AnywhenPlayer player)
        {
            AnywhenRuntime.AnysongPlayerBrain._players.Add(player);
        }

        private void HandleTransitionToPlayer(AnywhenPlayer player, TriggerBehaviour triggerBehaviour, TransitionMode transitionType)
        {
            _isStarted = true;
            if (AnywhenRuntime.IsPreviewing)
            {
                StopAllPlayers();
                player.Play();
                return;
            }


            switch (transitionType)
            {
                case TransitionMode.OvertakeInstant:
                    if (triggerBehaviour == TriggerBehaviour.StartPlayer)
                    {
                        StopAllPlayers();
                        player.Play();
                    }

                    if (triggerBehaviour == TriggerBehaviour.StopPlayer)
                    {
                        player.Stop();
                    }
                    break;
                case TransitionMode.OvertakeNextBar:
                    _nextPlayer = new NextUpPlayer(player, transitionType, triggerBehaviour);
                    break;
                case TransitionMode.MixInstant:
                    if (triggerBehaviour == TriggerBehaviour.StartPlayer)
                        player.Play();
                    if (triggerBehaviour == TriggerBehaviour.StopPlayer)
                        player.Stop();

                    break;
                case TransitionMode.MixNextBar:
                    _nextPlayer = new NextUpPlayer(player, transitionType, triggerBehaviour);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        

        public static void RegisterStop(AnywhenPlayer anywhenPlayer)
        {
            if (AnywhenRuntime.AnysongPlayerBrain._players.Contains(anywhenPlayer))
                AnywhenRuntime.AnysongPlayerBrain._players.Remove(anywhenPlayer);
        }
    }
}