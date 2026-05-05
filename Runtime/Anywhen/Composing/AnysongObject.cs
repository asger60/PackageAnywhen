using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Anywhen.Composing
{
    [CreateAssetMenu(fileName = "Anysong", menuName = "Anywhen/Anysong", order = 1)]
    public class AnysongObject : ScriptableObject
    {
        [Range(10, 200)] public int tempo;
        [Range(0, 1f)] public float songVolume = 1;


        public List<AnysongSection> Sections;
        public List<AnysongTrackSettings> Tracks;


        public AnywhenSnapshot snapshotA = new();
        public AnywhenSnapshot snapshotB = new();


        public string author = "Floppy Club";
        public event Action<int, int, int> OnSongMidiChanged;
        public event Action OnSongSettingsChanged;

        public event Action OnSongEffectsChanged;
        public event Action OnSongTracksChanged;
        public event Action OnSongSectionsChanged;

        private void OnValidate()
        {
            RefreshSettings();
            RefrestSections();
        }

        

        [ContextMenu("ClearPatterns")]
        void ClearPatterns()
        {
            Debug.Log("ClearPatterns");
            foreach (var section in Sections)
            {
                section.Init(Tracks);
            }
        }


        [ContextMenu("RandomizeStepWeights")]
        void RandomizeStepWeights()
        {
            foreach (var anySection in Sections)
            {
                foreach (var rSectionTrack in anySection.tracks)
                {
                    foreach (var pattern in rSectionTrack.patterns)
                    {
                        for (var i = 0; i < pattern.steps.Count; i++)
                        {
                            var step = pattern.steps[i];
                            var patternStep = step;
                            //patternStep.mixWeight = Random.Range(0, 1f);
                            pattern.steps[i] = patternStep;
                        }
                    }
                }
            }
        }





        public void RefreshMidi(int sectionIndex, int trackIndex, int patternIndex)
        {
            OnSongMidiChanged?.Invoke(sectionIndex, trackIndex, patternIndex);
        }

        public void RefreshSettings()
        {
            OnSongSettingsChanged?.Invoke();
        }

        public void RefreshEffects()
        {
            OnSongEffectsChanged?.Invoke();
        }

        public void RefrestTrack()
        {
            OnSongTracksChanged?.Invoke();
        }
        
        public void RefrestSections()
        {
            OnSongSectionsChanged?.Invoke();
        }


        public void RemoveListeners()
        {
            if (OnSongMidiChanged != null)
            {
                foreach (var currentDelegate in OnSongMidiChanged.GetInvocationList())
                {
                    OnSongMidiChanged -= (Action<int, int, int>)currentDelegate;
                }
            }

            if (OnSongSettingsChanged != null)
            {
                foreach (var currentDelegate in OnSongSettingsChanged.GetInvocationList())
                {
                    OnSongSettingsChanged -= (Action)currentDelegate;
                }
            }

            if (OnSongEffectsChanged != null)
            {
                foreach (var currentDelegate in OnSongEffectsChanged.GetInvocationList())
                {
                    OnSongEffectsChanged -= (Action)currentDelegate;
                }
            }

            if (OnSongTracksChanged != null)
            {
                foreach (var currentDelegate in OnSongTracksChanged.GetInvocationList())
                {
                    OnSongTracksChanged -= (Action)currentDelegate;
                }
            }
            
            if (OnSongSectionsChanged != null)
            {
                foreach (var currentDelegate in OnSongSectionsChanged.GetInvocationList())
                {
                    OnSongSectionsChanged -= (Action)currentDelegate;
                }
            }

            OnSongMidiChanged = null;
            OnSongSettingsChanged = null;
            OnSongEffectsChanged = null;
            OnSongTracksChanged = null;
            OnSongSectionsChanged = null;
        }
    }
}