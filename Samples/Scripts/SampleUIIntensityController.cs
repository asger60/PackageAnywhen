using Anywhen.Composing;
using UnityEngine;
using UnityEngine.UI;

public class SampleUIIntensityController : MonoBehaviour
{
    [SerializeField] Slider slider;

    private void Start()
    {
        slider.onValueChanged.AddListener(SliderUpdated);
    }

    private void SliderUpdated(float value)
    {
        AnysongPlayerBrain.SetGlobalIntensity(value);
    }
}
