using System;
using UnityEngine;
using UnityEngine.UI;

public class FillSelector : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 _shownPosition, _hiddenPosition, _currentPositionTarget;
    public Button[] buttons;

    private void Awake()
    {
        TryGetComponent(out _rectTransform);
        _shownPosition = _rectTransform.anchoredPosition;
        _hiddenPosition = _shownPosition + Vector2.left * 200;
        _rectTransform.anchoredPosition = _hiddenPosition;
        _currentPositionTarget = _hiddenPosition;
    }

    private void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var i1 = i;
            buttons[i].onClick.AddListener(() =>
            {
                GodHand.Instance.SetFillIndex(i1, buttons[i1].image.color);
            });
        }
    }

    private void Update()
    {
        _rectTransform.anchoredPosition =
            Vector2.Lerp(_rectTransform.anchoredPosition, _currentPositionTarget, Time.deltaTime * 5);
    }

    public void SetIsActive(bool state)
    {
        _currentPositionTarget = state ? _shownPosition : _hiddenPosition;
    }
}