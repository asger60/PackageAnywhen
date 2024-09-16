using UnityEngine;
using UnityEngine.UI;

namespace Samples.Scripts
{
    public class SampleUIInput : MonoBehaviour
    {
        [SerializeField] private SamplePlayerControls playerControls;
        [SerializeField] private SampleUIButtonEvents buttonForward, buttonReverse, buttonLeft, buttonRight, buttonShoot;
        [SerializeField] private Toggle playerIntensityControlToggle;
        void Start()
        {
            buttonForward.OnButtonStateChange += OnForwardButtonStateChange;
            buttonReverse.OnButtonStateChange += OnReverseButtonStateChange;
            buttonLeft.OnButtonStateChange += OnLeftButtonStateChange;
            buttonRight.OnButtonStateChange += OnRightButtonStateChange;
            buttonShoot.OnButtonStateChange += OnShootButtonStateChange;
            playerIntensityControlToggle.onValueChanged.AddListener(OnPlayerIntensityToggleChanged);
        }

        private void OnPlayerIntensityToggleChanged(bool state)
        {
            playerControls.SetIntensityControl(state);
        }

        private void OnForwardButtonStateChange(bool state)
        {
            playerControls.SetVerticalMovement(state ? 1 : 0);
        }

        private void OnReverseButtonStateChange(bool state)
        {
            playerControls.SetVerticalMovement(state ? -1 : 0);
        }

        private void OnLeftButtonStateChange(bool state)
        {
            playerControls.SetHorizontalMovement(state ? -1 : 0);
        }

        private void OnRightButtonStateChange(bool state)
        {
            playerControls.SetHorizontalMovement(state ? 1 : 0);
        }
        
        private void OnShootButtonStateChange(bool state)
        {
            playerControls.SetShooting(state);
        }
    }
}