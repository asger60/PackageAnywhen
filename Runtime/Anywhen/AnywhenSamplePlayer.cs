﻿using System;
using System.Collections.Generic;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

namespace Anywhen
{
    public class AnywhenSamplePlayer : MonoBehaviour
    {
        public AnywhenSampler anywhenSamplerPrefab;

        [SerializeField] private List<AnywhenSampler> _allSamplers = new();
        public int numberOfSamplers = 32;
        private bool _isInit;
        public bool IsInit => _isInit;
        public static AnywhenSamplePlayer Instance => AnywhenRuntime.AnywhenSamplerHandler;

        

        private void Start()
        {
            if (!AnywhenMetronome.Instance.IsInit) AnywhenMetronome.Instance.Init();
            Init();
        }

        public void Init()
        {
            _isInit = true;
            for (int i = 0; i < _allSamplers.Count; i++)
            {
                _allSamplers[i].Init();
            }
        }

        [ContextMenu("CreateSamplers")]
        internal void CreateSamplers()
        {
            _allSamplers.Clear();
            foreach (var sampler in transform.GetComponentsInChildren<AnywhenSampler>())
            {
                if (sampler.gameObject == gameObject) continue;
                DestroyImmediate(sampler.gameObject);
            }

            for (int i = 0; i < numberOfSamplers; i++)
            {
                _allSamplers.Add(Instantiate(anywhenSamplerPrefab, transform));
            }

        }


        private AnywhenSampler GetSampler()
        {
            foreach (var thisSampler in _allSamplers)
            {
                if (thisSampler.IsReady)
                {
                    thisSampler.SetReady(false);
                    return thisSampler;
                }
            }

            AnywhenRuntime.Log("didn't find a free sampler - returning the one with the oldest source");
            //didn't find a free sampler - returning the one with the oldest source
            float shortestDuration = float.MaxValue;
            AnywhenSampler oldestAnywhenSampler = null;
            foreach (var thisSampler in _allSamplers)
            {
                float thisDuration = thisSampler.GetDurationToEnd();
                if (thisDuration < shortestDuration)
                {
                    shortestDuration = thisDuration;
                    oldestAnywhenSampler = thisSampler;
                }
            }

            return oldestAnywhenSampler;
        }


        public void HandleEvent(NoteEvent e, AnywhenSampleInstrument anywhenInstrumentSettings, AnywhenMetronome.TickRate rate, AnysongTrack track = null)
        {
            switch (e.state)
            {
                case NoteEvent.EventTypes.NoteOff:

                    foreach (var thisSampler in _allSamplers)
                    {
                        if (thisSampler.Instrument != anywhenInstrumentSettings) continue;

                        if (thisSampler.IsPlaying || thisSampler.IsArmed)
                        {
                            double stopTime = rate == AnywhenMetronome.TickRate.None
                                ? 0
                                : AnywhenMetronome.Instance.GetScheduledPlaytime(rate);
                            thisSampler.NoteOff(stopTime);
                        }
                    }

                    break;

                case NoteEvent.EventTypes.NoteOn:

                    for (int i = 0; i < e.notes.Length; i++)
                    {
                        double playTime = 0;
                        var note = e.notes[i];
                        if (rate != AnywhenMetronome.TickRate.None)
                        {
                            playTime = AnywhenMetronome.Instance.GetScheduledPlaytime(rate) + e.drift;
                            playTime += e.chordStrum[i];

                            foreach (var thisSampler in _allSamplers)
                            {
                                if (thisSampler.Instrument != anywhenInstrumentSettings) continue;
                                if (thisSampler.IsArmed && thisSampler.CurrentNote == note && Math.Abs(thisSampler.ScheduledPlayTime - playTime) < .01f)
                                {
                                    return;
                                }
                            }
                        }

                        AnywhenSampler anywhenSampler = GetSampler();

                        if (anywhenSampler == null)
                        {
                            AnywhenRuntime.Log("no available samplers ");
                            return;
                        }


                        double stopTime = e.duration < 0
                            ? -1
                            : AnywhenMetronome.Instance.GetScheduledPlaytime(rate) + e.duration;

                        anywhenSampler.NoteOn(note, playTime, stopTime, e.velocity, anywhenInstrumentSettings, e.envelope, track);
                    }

                    break;


                case NoteEvent.EventTypes.NoteDown:

                    foreach (var thisSampler in _allSamplers)
                    {
                        if (thisSampler.Instrument != anywhenInstrumentSettings) continue;
                        foreach (var note in e.notes)
                        {
                            if (thisSampler.CurrentNote == note)
                                thisSampler.SetPitch(e.expression1);
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}