using System;
using Samples.Scripts;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FillSelector : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Vector2 _shownPosition, _hiddenPosition, _currentPositionTarget;
    public Button[] buttons;
    private readonly Vector2[] _buttonTargets = new Vector2[4];
    private Vector2 _modifyHeaderTarget;

    [FormerlySerializedAs("hideButton3and4")]
    public bool hideButton3And4 = false;

    private int _currentIndex;
    private IMixableObject _mixableObject;
    public RectTransform modifyHeader;

    private void Awake()
    {
        TryGetComponent(out _rectTransform);
        _shownPosition = _rectTransform.anchoredPosition;
        _hiddenPosition = _shownPosition + Vector2.left * 300;
        _rectTransform.anchoredPosition = _hiddenPosition;
        _currentPositionTarget = _hiddenPosition;
    }

    public void Init(IMixableObject mixableObject)
    {
        print("init " + mixableObject);
        _mixableObject = mixableObject;
    }

    private void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            var i1 = i;
            buttons[i].onClick.AddListener(() => { SetFillIndex(i1); });
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

        modifyHeader.anchoredPosition =
            Vector2.Lerp(modifyHeader.anchoredPosition, _modifyHeaderTarget, Time.deltaTime * 10f);

        _rectTransform.anchoredPosition =
            Vector2.Lerp(_rectTransform.anchoredPosition, _currentPositionTarget, Time.deltaTime * 15);
    }

    public void SetIsActive(bool state)
    {
        _currentPositionTarget = state ? _shownPosition : _hiddenPosition;
    }

    public void SetFillIndex(int index)
    {
        _currentIndex = index;

        OnMixDone();

        if (index == -1)
        {
            return;
        }

        GodHand.Instance.SetFillIndex(index, buttons[index].image.color);
    }

    public void OnMixDone()
    {
        if (_mixableObject == null)
        {
            return;
        }

        for (var i = 0; i < _buttonTargets.Length; i++)
        {
            if (i > 1 && hideButton3And4 && _mixableObject.GetMixValueForTrack(0) <= 0.05f)
            {
                _buttonTargets[i] = Vector2.left * 300;

                continue;
            }

            _buttonTargets[i] = (i == _currentIndex) ? Vector2.right * 20 : Vector2.zero;
        }

        if (hideButton3And4 && _mixableObject.GetMixValueForTrack(0) <= 0.05f)
            _modifyHeaderTarget = Vector2.left * 300;
        else
        {
            _modifyHeaderTarget = Vector2.zero;
        }
    }
}