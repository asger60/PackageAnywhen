using System.Collections.Generic;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
    public class AnywhenSynthPlayer : MonoBehaviour
    {
        public AnywhenSynth anywhenSynthPrefab;

        [SerializeField] private List<AnywhenSynth> _allSynths = new();
        public int numberOfSynths = 32;
        private bool _isInit;
        public bool IsInit => _isInit;


        private void Awake()
        {
            Init();
        }

        public void Init()
        {
            for (int i = 0; i < numberOfSynths; i++)
            {
                _allSynths[i].Init();
            }

            _isInit = true;
        }

        [ContextMenu("CreateSynths")]
        internal void CreateSynths()
        {
            _allSynths.Clear();


            foreach (var synth in transform.GetComponentsInChildren<AnywhenSynth>())
            {
                if (synth.gameObject == gameObject) continue;
                DestroyImmediate(synth.gameObject);
            }

            for (int i = 0; i < numberOfSynths; i++)
            {
                _allSynths.Add(Instantiate(anywhenSynthPrefab, transform));
            }
        }

        private AnywhenSynth GetSynth(AnywhenSynthPreset synthPreset)
        {
            foreach (var synth in _allSynths)
            {
                if (!synth.Preset) continue;
                if (synth.Preset.name == synthPreset.name) return synth;
            }


            return null;
        }


        public void HandleEvent(NoteEvent e, AnywhenSynthPreset synthPreset, AnywhenMetronome.TickRate rate, AudioMixerGroup mixerChannel = null)
        {
            var thisSynth = GetSynth(synthPreset);

            if (thisSynth)
            {
                double playTime = 0;

                if (rate != AnywhenMetronome.TickRate.None)
                {
                    playTime = AnywhenMetronome.Instance.GetScheduledPlaytime(rate) + e.drift;
                }


                if (e is { state: NoteEvent.EventTypes.NoteOn, duration: > 0 })
                {
                    e.notes = AnywhenConductor.Instance.GetScaledNotes(e.notes);
                    thisSynth.HandleEventScheduled(e, playTime);

                    var noteOff = new NoteEvent(NoteEvent.EventTypes.NoteOff);
                    double stopTime = playTime;
                    decimal dec = new decimal(e.duration);
                    stopTime += (double)dec;
                    thisSynth.HandleEventScheduled(noteOff, stopTime);
                }
            }
            else
            {
                Debug.Log("failed to get synth");
            }
        }

        public void ClearPresets()
        {
            foreach (var synth in _allSynths)
            {
                synth.SetPreset(null);
            }
        }

        public void RegisterPreset(AnywhenSynthPreset synthPreset)
        {
            foreach (var synth in _allSynths)
            {
                if (!synth.Preset)
                {
                    synth.SetPreset(synthPreset);
                    break;
                }
            }
        }
    }
}