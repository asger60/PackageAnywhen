using System.Collections.Generic;
using System.Linq;
using Rytmos;
using Rytmos.AudioSystem;
using Rytmos.JamMode.JamModeObjectTypes;
using UnityEngine;
using UnityEngine.Audio;

namespace PackageAnywhen.Runtime.Anywhen.AudioSystem
{
    public class MultiSamplePlayer : MonoBehaviour
    {
        public Sampler samplerPrefab;

        private readonly List<Sampler> _allSamplers = new List<Sampler>(50);

        private bool _isInit;
        public bool IsInit => _isInit;
        public static MultiSamplePlayer Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!AnywhenMetronome.Instance.IsInit) AnywhenMetronome.Instance.Init();


            for (int i = 0; i < 50; i++)
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
                if (thisSampler.IsReady && !thisSampler.IsStopping)
                    return thisSampler;
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

        

        public void HandleEvent(NoteEvent e, JamModePercussion instrumentSettings, AudioMixerGroup mixerChannel)
        {
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

                        double timing = AnywhenMetronome.Instance.GetScheduledPlaytime(e.quantization)+ e.drift + e.chordStrum[i];
                        
                        if (note < (instrumentSettings.instrumentObjects.Length) && note >= 0)
                        {
                            sampler.NoteOn(0, timing, e.velocity, instrumentSettings.instrumentObjects[note], mixerChannel);
                        }
                        else
                        {
                            print("note out of range");
                        }
                    }


                    break;
                case NoteEvent.EventTypes.NoteOff:
                    foreach (var thisSampler in _allSamplers)
                    {
                        if (!thisSampler.IsStopping && thisSampler.Settings == instrumentSettings.instrumentObjects[0])
                            thisSampler.NoteOff(AnywhenMetronome.Instance.GetScheduledPlaytime(e.quantization));
                    }
                    break;
                case NoteEvent.EventTypes.NoteDown:
                    break;
            }
        }
    }
}