using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Samples
{
    public class SampleUIButtonEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> OnButtonStateChange;
        public KeyCode inputKey;
        
        public void OnPointerDown(PointerEventData eventData)
        {
            OnButtonStateChange?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnButtonStateChange?.Invoke(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(inputKey))
            {
                OnButtonStateChange?.Invoke(true);
            }
            if (Input.GetKeyUp(inputKey))
            {
                OnButtonStateChange?.Invoke(false);
            }
        }
    }
}
