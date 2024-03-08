using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Anywhen
{
    public class AnywhenSynthPlayer : MonoBehaviour
    {
        public Synth.Synth.AnywhenSynth anywhenSynthPrefab;

        private readonly List<Synth.Synth.AnywhenSynth> _allSynths = new();
        public int numberOfSynths = 32;
        private bool _isInit;
        public bool IsInit => _isInit;
 

        private void Awake()
        {
            for (int i = 0; i < numberOfSynths; i++)
            {
                _allSynths.Add(Instantiate(anywhenSynthPrefab, transform));
            }

            _isInit = true;
            LateInit();
        }
        
        async void LateInit()
        {
            while (AnywhenMetronome.Instance == null)
            {
                await Task.Yield();
            }
            print("metronome init");

        }


        private Synth.Synth.AnywhenSynth GetSynth(AnywhenSynthPreset synthPreset)
        {
            foreach (var synth in _allSynths)
            {
                if (synth.Preset == synthPreset) return synth;
            }


            return null;
        }


        public void HandleEvent(NoteEvent e, AnywhenSynthPreset synthPreset,
            AnywhenMetronome.TickRate rate, AudioMixerGroup mixerChannel = null)
        {
            var thisSynth = GetSynth(synthPreset);
            if (thisSynth)
            {
                double playTime = 0;
                if (rate != AnywhenMetronome.TickRate.None)
                {
                    playTime = AnywhenMetronome.Instance.GetScheduledPlaytime(rate) + e.drift;
                }

                e.notes = AnywhenConductor.Instance.GetScaledNotes(e.notes);

                thisSynth.HandleEventScheduled(e, playTime);
                
                if (e is { state: NoteEvent.EventTypes.NoteOn, duration: > 0 })
                {
                    var noteOff = new NoteEvent(NoteEvent.EventTypes.NoteOff);
                    thisSynth.HandleEventScheduled(noteOff, playTime + e.duration);
                }
            }
        }

        public void RegisterPreset(AnywhenSynthPreset synthPreset)
        {
            foreach (var synth in _allSynths)
            {
                if (synth.Preset == null)
                {
                    print("set synth " + synthPreset.name);
                    synth.SetPreset(synthPreset);
                    break;
                }
            }
        }
    }
}