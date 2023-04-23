using System;
using UnityEngine;
using UnityEngine.UI;

public class FillSelector : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 _shownPosition, _hiddenPosition, _currentPositionTarget;
    public Button[] buttons;
    private readonly Vector2[] _buttonTargets = new Vector2[4];
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
                SetFillIndex(i1);
            });
        }
    }

    private void Update()
    {
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var buttonRect = button.transform as RectTransform;
            buttonRect.anchoredPosition =
                Vector2.Lerp(buttonRect.anchoredPosition, _buttonTargets[i], Time.deltaTime * 10f);
        }

        _rectTransform.anchoredPosition =
            Vector2.Lerp(_rectTransform.anchoredPosition, _currentPositionTarget, Time.deltaTime * 5);
    }

    public void SetIsActive(bool state)
    {
        _currentPositionTarget = state ? _shownPosition : _hiddenPosition;
    }

    public void SetFillIndex(int index)
    {
        if (index == -1)
        {
            for (var i = 0; i < _buttonTargets.Length; i++)
            {
                _buttonTargets[i] = (i == index) ? Vector2.right * 20 : Vector2.zero;
            }
            return;
        }
        GodHand.Instance.SetFillIndex(index, buttons[index].image.color);
        for (var i = 0; i < _buttonTargets.Length; i++)
        {
            _buttonTargets[i] = (i == index) ? Vector2.right * 20 : Vector2.zero;
        }
    }
}