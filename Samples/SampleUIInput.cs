using UnityEngine;

namespace Samples
{
    public class SampleUIInput : MonoBehaviour
    {
        [SerializeField] private SamplePlayerControls playerControls;
        [SerializeField] private SampleUIButtonEvents buttonForward, buttonReverse, buttonLeft, buttonRight;

        void Start()
        {
            buttonForward.OnButtonStateChange += OnForwardButtonStateChange;
            buttonReverse.OnButtonStateChange += OnReverseButtonStateChange;
            buttonLeft.OnButtonStateChange += OnLeftButtonStateChange;
            buttonRight.OnButtonStateChange += OnRightButtonStateChange;
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
    }
}