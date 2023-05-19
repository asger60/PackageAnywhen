﻿using System;
using System.Collections.Generic;
using System.Linq;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    public class SamplePlayer : MonoBehaviour
    {
        public Sampler samplerPrefab;

        private readonly List<Sampler> _allSamplers = new List<Sampler>(1000);

        private bool _isInit;
        public bool IsInit => _isInit;
        public int activeSamplePlayers;
        public static SamplePlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogWarning("Sampleplayer is missing, please add to scene");
                }

                return _instance;
            }
        }

        private static SamplePlayer _instance;

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            activeSamplePlayers = 0;
            foreach (var sampler in _allSamplers)
            {
                if (!sampler.IsReady)
                    activeSamplePlayers++;
            }
        }

        private void Start()
        {
            if (!AnywhenMetronome.Instance.IsInit) AnywhenMetronome.Instance.Init();


            for (int i = 0; i < 1000; i++)
            {
                _allSamplers.Add(Instantiate(samplerPrefab, transform));
                _allSamplers.Last().Init(AnywhenMetronome.TickRate.Sub32);
            }

            _isInit = true;
        }


        private Sampler GetSampler()
        {
            foreach (var thisSampler in _allSamplers)
            {
                if (thisSampler.IsReady)
                {
                    thisSampler.SetReady(false);
                    return thisSampler;
                }
            }

            print("#AudioSystem#didn't find a free sampler - returning the one with the oldest source");
            //didn't find a free sampler - returning the one with the oldest source
            float shortestDuration = float.MaxValue;
            Sampler oldestSampler = null;
            foreach (var thisSampler in _allSamplers)
            {
                float thisDuration = thisSampler.GetDurationToEnd();
                if (thisDuration < shortestDuration)
                {
                    shortestDuration = thisDuration;
                    oldestSampler = thisSampler;
                }
            }

            return oldestSampler;
        }


        public void HandleEvent(NoteEvent e, AnywhenInstrument anywhenInstrumentSettings,
            AnywhenMetronome.TickRate rate, AudioMixerGroup mixerChannel = null)
        {
            float drift = 0;

            switch (e.state)
            {
                case NoteEvent.EventTypes.NoteOn:
                    for (int i = 0; i < e.notes.Length; i++)
                    {
                        var note = e.notes[i];
                        Sampler sampler = GetSampler();

                        if (sampler == null)
                        {
                            Debug.LogWarning("no available samplers ");
                            return;
                        }

                        double timing = AnywhenMetronome.Instance.GetScheduledPlaytime(rate) + e.drift +
                                        e.chordStrum[i];
                        sampler.NoteOn(note, timing, e.velocity, anywhenInstrumentSettings, mixerChannel);
                    }


                    break;
                case NoteEvent.EventTypes.NoteOff:

                    if (anywhenInstrumentSettings.instrumentType == AnywhenInstrument.InstrumentType.Sustained)
                    {
                        foreach (var thisSampler in _allSamplers)
                        {
                            if (thisSampler.Settings == anywhenInstrumentSettings)
                                thisSampler.NoteOff(AnywhenMetronome.Instance.GetScheduledPlaytime(rate) +
                                                    drift);
                        }
                    }


                    break;
                case NoteEvent.EventTypes.NoteDown:
                    break;
            }
        }
    }
}