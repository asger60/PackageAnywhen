using System;
using System.Collections.Generic;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;
using UnitySynth.Runtime.AudioSystem;

namespace Anywhen
{
    public class AnywhenSynthPlayer : MonoBehaviour
    {
        public Synth.Synth.AnywhenSynth anywhenSynthPrefab;

        private readonly List<Synth.Synth.AnywhenSynth> _allSynths = new();
        public int numberOfSynths = 32;
        private bool _isInit;
        public bool IsInit => _isInit;
        public int activeSynths;


        private void Update()
        {
            activeSynths = 0;
            foreach (var sampler in _allSynths)
            {
                // if (!sampler.IsReady)
                //     activeSynths++;
            }
        }

        private void Start()
        {
            if (!AnywhenMetronome.Instance.IsInit) AnywhenMetronome.Instance.Init();

            for (int i = 0; i < numberOfSynths; i++)
            {
                _allSynths.Add(Instantiate(anywhenSynthPrefab, transform));
            }

            _isInit = true;
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
            }

            
        }

        public void RegisterPreset(AnywhenSynthPreset synthPreset)
        {
            foreach (var synth in _allSynths)
            {
                if (synth.Preset == null)
                {
                    synth.SetPreset(synthPreset);
                    break;
                }
            }
        }
    }
}