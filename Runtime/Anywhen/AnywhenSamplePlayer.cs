using System.Collections.Generic;
using System.Linq;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Anywhen
{
    public class AnywhenSamplePlayer : MonoBehaviour
    {
        [FormerlySerializedAs("samplerPrefab")] public AnywhenSampler anywhenSamplerPrefab;

        private readonly List<AnywhenSampler> _allSamplers = new List<AnywhenSampler>(1000);

        private bool _isInit;
        public bool IsInit => _isInit;
        public int activeSamplePlayers;
        public static AnywhenSamplePlayer Instance => AnywhenRuntime.AnywhenSamplePlayer;

        

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
                _allSamplers.Add(Instantiate(anywhenSamplerPrefab, transform));
                _allSamplers.Last().Init(AnywhenMetronome.TickRate.Sub32);
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
                        AnywhenSampler anywhenSampler = GetSampler();

                        if (anywhenSampler == null)
                        {
                            Debug.LogWarning("no available samplers ");
                            return;
                        }

                        double timing = AnywhenMetronome.Instance.GetScheduledPlaytime(rate) + e.drift +
                                        e.chordStrum[i];
                        anywhenSampler.NoteOn(note, timing, e.velocity, anywhenInstrumentSettings, mixerChannel);
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