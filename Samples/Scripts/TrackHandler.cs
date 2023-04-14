using System;
using Samples.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace PackageAnywhen.Samples.Scripts
{
    public class TrackHandler : MonoBehaviour
    {
        public int activeTrack;


        public DrumPatternMixer drumPatternMixer;
        public BassPatternMixer bassPatternMixer;

        public Button buttonDrums, buttonBass;
        public Slider uiSlider1, uiSlider2;

        private void Start()
        {
            buttonDrums.onClick.AddListener(() =>
            {
                uiSlider1.value = drumPatternMixer.currentPatternMix;
                uiSlider2.value = drumPatternMixer.currentInstrumentMix;
                activeTrack = 0;
            });
            buttonBass.onClick.AddListener(() =>
            {
                uiSlider1.value = bassPatternMixer.currentPatternMix;
                uiSlider2.value = bassPatternMixer.currentMelodyMix;
                activeTrack = 1;
            });
        }

        private void Update()
        {
            drumPatternMixer.SetPartyDudesActive(activeTrack == 0);
            bassPatternMixer.SetPartyDudesActive(activeTrack == 1);
            switch (activeTrack)
            {
                case 0:
                    drumPatternMixer.currentPatternMix = uiSlider1.value;
                    drumPatternMixer.currentInstrumentMix = uiSlider2.value;
                    break;
                case 1:
                    bassPatternMixer.currentPatternMix = uiSlider1.value;
                    bassPatternMixer.currentMelodyMix = uiSlider2.value;
                    break;
            }
        }
    }
}