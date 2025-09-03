using System;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Anywhen
{
    public class AnywhenMetronome : MonoBehaviour
    {
        [SerializeField] int Bpm = 100;
        [SerializeField] bool Playing;
        [SerializeField] int sub2;
        [SerializeField] int sub4;
        [SerializeField] int sub8;
        [SerializeField] public int Sub16;


        private double _sub16Length;
        private double _sub8Length;

        private double _sub4Length;
        private double _sub2Length;

        private double _nextTime16 = 0;
        private double _nextTime8 = 0;
        private double _nextTime4 = 0;
        private double _nextTime2 = 0;

        public double bufferTime = 0.2f;


        public enum TickRate
        {
            None = 0,
            Sub2 = 2,
            Sub4 = 4,
            Sub8 = 8,
            Sub16 = 16,
        }

        public Action OnTick2;
        public Action OnTick4;
        public Action OnTick8;

        public Action OnTick16;

        public Action OnNextBar;

        private int _currentBar;
        public int CurrentBar => _currentBar;
        private bool _isInit;
        public bool IsInit => _isInit;


        public static AnywhenMetronome Instance => AnywhenRuntime.Metronome;
        public bool debugMode;
        private bool _isStopped;


        [Serializable]
        public struct DebugSettings
        {
            public bool debugBar, debug2, debug4, debug8, debug16;
            public AnywhenSampleInstrument debugAnywhenInstrument;
        }

        public DebugSettings debugSettings;
        public int GetTempo() => Bpm;

        private void Start()
        {
            Init();
            _currentBar = 0;
            _nextTime16 = AudioSettings.dspTime + bufferTime;
            _nextTime8 = _nextTime16;
            _nextTime4 = _nextTime16;
            _nextTime2 = _nextTime16;
            Play();
        }

        public void SetTempo(int newBpm)
        {
            Bpm = newBpm;
            Init();
        }


        public void Init()
        {
            _sub4Length = ((float)60000 / Bpm) / 1000;
            _sub2Length = _sub4Length * 2;

            _sub8Length = _sub4Length * 0.5f;
            _sub16Length = _sub4Length * 0.25f;
            _isInit = true;
        }


        public void Play()
        {
            Init();
            _isStopped = false;
            Playing = true;

            _currentBar = 0;
            Sub16 = 0;
            sub8 = 0;
            sub4 = 0;
            sub2 = 0;
            _nextTime16 = AudioSettings.dspTime + bufferTime;

            OnNextBar?.Invoke();
            OnTick16 += DebugOnTick16;
        }


        private void DebugOnTick16()
        {
            if (debugSettings.debug16)
            {
                NoteEvent e = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
                AnywhenSamplePlayer.Instance.HandleEvent(e, debugSettings.debugAnywhenInstrument, TickRate.Sub16);
            }

            if (debugSettings.debugBar && Sub16 == 0)
            {
                NoteEvent e = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
                AnywhenSamplePlayer.Instance.HandleEvent(e, debugSettings.debugAnywhenInstrument, TickRate.Sub16);
            }

            if (debugSettings.debug2 && Sub16 % 8 == 0)
            {
                NoteEvent e = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
                AnywhenSamplePlayer.Instance.HandleEvent(e, debugSettings.debugAnywhenInstrument, TickRate.Sub16);
            }

            if (debugSettings.debug4 && Sub16 % 4 == 0)
            {
                NoteEvent e = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
                AnywhenSamplePlayer.Instance.HandleEvent(e, debugSettings.debugAnywhenInstrument, TickRate.Sub16);
            }

            if (debugSettings.debug8 && Sub16 % 2 == 0)
            {
                NoteEvent e = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
                AnywhenSamplePlayer.Instance.HandleEvent(e, debugSettings.debugAnywhenInstrument, TickRate.Sub16);
            }
        }

        public void Update()
        {
            if (!Playing) return;
            if (_isStopped) return;
            if (!(AudioSettings.dspTime + bufferTime >= _nextTime16)) return;
            OnTick16?.Invoke();

            Sub16++;


            if (Sub16 % 2 == 0)
            {
                _nextTime8 = _nextTime16 + _sub8Length;
                OnTick8?.Invoke();
                sub8++;
            }

            if (Sub16 % 4 == 0)
            {
                _nextTime4 = _nextTime16 + _sub4Length;
                OnTick4?.Invoke();
                sub4++;
            }

            if (Sub16 % 8 == 0)
            {
                _nextTime2 = _nextTime16 + _sub2Length;
                OnTick2?.Invoke();
                sub2++;
            }

            if (Sub16 % 16 == 0)
            {
                _currentBar++;
                OnNextBar?.Invoke();
            }

            // reset
            if (Sub16 == 16)
            {
                // Sub32 = 0;
                Sub16 = 0;
                sub8 = 0;
                sub4 = 0;
                sub2 = 0;
            }


            _nextTime16 += _sub16Length;

            if (_nextTime16 <= AudioSettings.dspTime + bufferTime)
            {
                if (debugMode)
                    Debug.LogWarning("Buffertime is too big");
            }
        }


        public double GetLength(TickRate tickRate)
        {
            switch (tickRate)
            {
                case TickRate.Sub2:
                    return _sub2Length;
                case TickRate.Sub4:
                    return _sub4Length;
                case TickRate.Sub8:
                    return _sub8Length;
                case TickRate.Sub16:
                    return _sub16Length;
                //case TickRate.Sub32:
                //    return _sub32Length;
                default:
                    //Debug.Log("trying to fetch ");
                    return 0;
            }
        }


        public double GetScheduledPlaytime(TickRate playbackRate)
        {
            return playbackRate switch
            {
                TickRate.None => AudioSettings.dspTime,
                TickRate.Sub2 => _nextTime2,
                TickRate.Sub4 => _nextTime4,
                TickRate.Sub8 => _nextTime8,
                TickRate.Sub16 => _nextTime16,
                //TickRate.Sub32 => _nextTime32 + _sub32Length,
                _ => throw new ArgumentOutOfRangeException(nameof(playbackRate), playbackRate, null)
            };
        }

        public int GetCountForTickRate(TickRate tickRate)
        {
            return (Sub16 / (16 / (int)tickRate));
        }

        public float GetTimeToNextPlay(TickRate playbackRate)
        {
            return (float)GetScheduledPlaytime(playbackRate) - (float)AudioSettings.dspTime;
        }


        public void Stop()
        {
            _isStopped = true;
        }

        public Action GetCallBackForTickRate(TickRate tickRate)
        {
            return (tickRate) switch
            {
                TickRate.None => null,
                TickRate.Sub2 => OnTick2,
                TickRate.Sub4 => OnTick4,
                TickRate.Sub8 => OnTick8,
                TickRate.Sub16 => OnTick16,
                //TickRate.Sub32 => OnTick32,
                _ => throw new ArgumentOutOfRangeException(nameof(tickRate), tickRate, null)
            };
        }

        public static double GetTiming(TickRate playbackRate, float swingAmount, float humanizeAmount)
        {
            float subLength = (float)Instance.GetLength(playbackRate) / 2f;
            float drift = Random.Range(subLength * -1, subLength);
            float swing = 0;

            if (swingAmount > 0)
            {
                int count = Instance.GetCountForTickRate(playbackRate);
                if (count % 2 != 0)
                    swing = subLength * swingAmount;
            }

            return Mathf.Lerp(0, drift, humanizeAmount) + swing;
        }
    }
}