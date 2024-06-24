using System;
using System.Collections.Generic;
using System.Linq;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    public class AnywhenSamplePlayer : MonoBehaviour
    {
        public AnywhenSampler anywhenSamplerPrefab;

        private readonly List<AnywhenSampler> _allSamplers = new();
        public int numberOfSamplers = 32;
        private bool _isInit;
        public bool IsInit => _isInit;
        public int activeSamplePlayers;
        public static AnywhenSamplePlayer Instance => AnywhenRuntime.AnywhenSamplerHandler;


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


            for (int i = 0; i < numberOfSamplers; i++)
            {
                _allSamplers.Add(Instantiate(anywhenSamplerPrefab, transform));
                _allSamplers.Last().Init(AnywhenMetronome.TickRate.Sub16);
            }

            _isInit = true;
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

            print("#AudioSystem#didn't find a free sampler - returning the one with the oldest source");
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


        public void HandleEvent(NoteEvent e, AnywhenSampleInstrument anywhenInstrumentSettings,
            AnywhenMetronome.TickRate rate, AnysongTrack track = null)
        {
            switch (e.state)
            {
                case NoteEvent.EventTypes.NoteOff:
                    if (anywhenInstrumentSettings.instrumentType != AnywhenSampleInstrument.InstrumentType.Sustained)
                    {
                        //Debug.LogWarning("trying to stop an instrument that is not set to sustained");
                        return;
                    }

                    foreach (var thisSampler in _allSamplers)
                    {
                        if (thisSampler.Instrument != anywhenInstrumentSettings) continue;
                        if (e.notes.Length == 1 && e.notes[0] == -1000)
                        {
                            double stopTime = rate == AnywhenMetronome.TickRate.None
                                ? 0
                                : AnywhenMetronome.Instance.GetScheduledPlaytime(rate);

                            thisSampler.NoteOff(stopTime);
                        }
                        else
                        {
                            foreach (var note in e.notes)
                            {
                                if (thisSampler.CurrentNote == note)
                                {
                                    double stopTime = rate == AnywhenMetronome.TickRate.None
                                        ? 0
                                        : AnywhenMetronome.Instance.GetScheduledPlaytime(rate);

                                    thisSampler.NoteOff(stopTime);
                                }
                            }
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
                                if (thisSampler.IsArmed && thisSampler.CurrentNote == note &&
                                    thisSampler.ScheduledPlayTime == playTime)
                                {
                                    return;
                                }
                            }
                        }

                        AnywhenSampler anywhenSampler = GetSampler();

                        if (anywhenSampler == null)
                        {
                            //Debug.LogWarning("no available samplers ");
                            return;
                        }

                        

                        double stopTime = e.duration < 0
                            ? -1
                            : AnywhenMetronome.Instance.GetScheduledPlaytime(rate) + e.duration;

                        anywhenSampler.NoteOn(note, playTime, stopTime, e.velocity, anywhenInstrumentSettings, track);
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