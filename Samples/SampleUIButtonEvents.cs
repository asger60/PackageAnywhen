using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Samples
{
    public class SampleUIButtonEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> OnButtonStateChange;
        public void OnPointerDown(PointerEventData eventData)
        {
            OnButtonStateChange?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnButtonStateChange?.Invoke(false);
        }
    }
}
