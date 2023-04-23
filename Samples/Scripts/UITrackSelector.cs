using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITrackSelector : MonoBehaviour
{
    RectTransform _rectTransform => transform as RectTransform;


    private Vector2 _onScreenPosition;
    private Vector2 _hiddenPosition;
    private Vector2 _currentPositionTarget;

    private void Start()
    {
        _onScreenPosition = _rectTransform.anchoredPosition;
        _hiddenPosition = _onScreenPosition + Vector2.down * 300;
    }

    private void Update()
    {
        _rectTransform.anchoredPosition =
            Vector2.Lerp(_rectTransform.anchoredPosition, _currentPositionTarget, Time.deltaTime * 10);
    }

    public void SetIsActive(bool state)
    {
        _currentPositionTarget = state ? _onScreenPosition : _hiddenPosition;
    }
}
