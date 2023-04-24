using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFakeCursor : MonoBehaviour
{
    RectTransform _rectTransform => transform as RectTransform;
    public Canvas canvas;

    
    private void Update()
    {
        
        _rectTransform.anchoredPosition = Input.mousePosition / canvas.scaleFactor;
    }

    public void SetIsActive(bool state)
    {
        gameObject.SetActive(state);
    }
}