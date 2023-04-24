using System;
using System.Collections;
using System.Collections.Generic;
using Samples.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace PackageAnywhen.Samples.Scripts
{
    public class TrackHandler : MonoBehaviour
    {
        public int activeTrack;

        //public Button buttonDrums, buttonBass, buttonChords;
        public UISet[] uiSets;
        private List<IMixableObject> _mixTargets = new();
        public UIToggleGroup trackToggleGroup;
        public static TrackHandler Instance => _instance;
        private static TrackHandler _instance;
        public DrumPatternMixer drumPatternMixer;
        public BassPatternMixer bassPatternMixer;
        public ChordPatternMixer chordPatternMixer;

        private void Awake()
        {
            _instance = this;
            _mixTargets = new List<IMixableObject>(3);
            for (int i = 0; i < 3; i++)
            {
                _mixTargets.Add(null);
            }
        }

        private void Start()
        {
            trackToggleGroup.OnSelect = OnSelectTrack;
            
        }

        private bool _didInit;

        void Init()
        {
            if (_didInit) return;
            for (var i = 0; i < uiSets.Length; i++)
            {
                var uiSet = uiSets[i];
                uiSet.Init(_mixTargets[i]);
            }
            _didInit = true;
        }

        private void OnSelectTrack(int index)
        {
            SetActiveTrack(index);
        }

        IEnumerator WaitAndActivate()
        {
            yield return new WaitForSeconds(1);
            SetActiveTrack(2);
            trackToggleGroup.SelectButton(2, false);
        }

        public void AttachMixInterface(IMixableObject mixableObject, int index)
        {
            _mixTargets[index] = (mixableObject);
            int targetsAttached = 0;
            foreach (var mixTarget in _mixTargets)
            {
                if (mixTarget != null) targetsAttached++;
            }

            if (targetsAttached == 3)
                Init();
        }


        public void SetActiveTrack(int trackIndex)
        {
            for (int i = 0; i < uiSets.Length; i++)
            {
                uiSets[i].SetIsActive(i == trackIndex);
                _mixTargets[i]?.SetIsActive(i == trackIndex);
            }

            activeTrack = trackIndex;
        }

        public void Mix(int pattern, int stepIndex)
        {
            _mixTargets[activeTrack].Mix(pattern, stepIndex);
        }

        public void OnMixDone()
        {
            uiSets[activeTrack].fillSelector.OnMixDone();
        }

        private int _prevActiveTrack;

        public void HideTrackInterfaces()
        {
            _prevActiveTrack = activeTrack;
            for (int i = 0; i < uiSets.Length; i++)
            {
                uiSets[i].SetIsActive(false);
                _mixTargets[i]?.SetIsActive(false);
            }

            activeTrack = -1;
        }

        public void ShowTrackInterfaces()
        {
            SetActiveTrack(_prevActiveTrack);
        }

        public void SetInstrumentSetIndex(int index)
        {
            chordPatternMixer.SetInstrument(index);
            bassPatternMixer.SetInstrument(index);
        }
    }
}