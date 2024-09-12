using System;
using Anywhen.Composing;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Scripts
{
    public class SampleIntensityView : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        private float _intensityValue;

        private void Start()
        {
            AnysongPlayerBrain.OnIntensityChanged += OnIntensityChanged;
        }

        private void OnIntensityChanged(float value)
        {
            _intensityValue = value;
        }

        private void Update()
        {
            slider.value = Mathf.Lerp(slider.value, _intensityValue, Time.deltaTime * 10);
        }
    }
}