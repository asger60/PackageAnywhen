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

        public Button buttonDrums, buttonBass, buttonChords;
        public UISet[] uiSets;
        private List<IMixableObject> _mixTargets = new();

        public static TrackHandler Instance => _instance;
        private static TrackHandler _instance;

        private void Awake()
        {
            _instance = this;
        
            _mixTargets = new List<IMixableObject>(4);
            for (int i = 0; i < 4; i++)
            {
                _mixTargets.Add(null);
            }

            buttonDrums.onClick.AddListener(() => { SetActiveTrack(0); });
            buttonBass.onClick.AddListener(() => { SetActiveTrack(1); });
            buttonChords.onClick.AddListener(() => { SetActiveTrack(2); });
            StartCoroutine(WaitAndActivate());
        }

        IEnumerator WaitAndActivate()
        {
            yield return new WaitForSeconds(1);
            SetActiveTrack(2);
        }

        public void AttachMixInterface(IMixableObject mixableObject, int index)
        {
            _mixTargets[index] = (mixableObject);
        }


        void SetActiveTrack(int trackIndex)
        {
            for (int i = 0; i < uiSets.Length; i++)
            {
                uiSets[i].SetIsActive(i == trackIndex);
                _mixTargets[i].SetIsActive(i == trackIndex);
            }

            activeTrack = trackIndex;
        }

        public void Mix(int pattern, int stepIndex)
        {
            _mixTargets[activeTrack].Mix(pattern, stepIndex);
        }
    }
}