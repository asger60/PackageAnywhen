using UnityEngine;
using UnityEngine.UI;

public class MixView : MonoBehaviour
{
    public Image[] fillImages;

    public float[] fillAmountTargets;

    private float[] _currentFillAmounts = new float[4];
    private RectTransform _rectTransform;
    private Vector2 _shownPosition, _hiddenPosition, _currentPositionTarget;

    private void Awake()
    {
        TryGetComponent(out _rectTransform);
        _shownPosition = _rectTransform.anchoredPosition;
        _hiddenPosition = _shownPosition + Vector2.up * 200;
        _rectTransform.anchoredPosition = _hiddenPosition;
        _currentPositionTarget = _hiddenPosition;
    }

    private void Update()
    {
        _rectTransform.anchoredPosition =
            Vector2.Lerp(_rectTransform.anchoredPosition, _currentPositionTarget, Time.deltaTime * 5);
        
        float elapsedFill = 0;
        for (var i = 0; i < fillImages.Length; i++)
        {
            _currentFillAmounts[i] = Mathf.Lerp(_currentFillAmounts[i], fillAmountTargets[i], Time.deltaTime * 5);
            var image = fillImages[i];
            image.fillAmount = _currentFillAmounts[i];
            var transformRotation = fillImages[i].transform.rotation;
            transformRotation.eulerAngles = Vector3.forward * Mathf.Lerp(0, -360, elapsedFill);
            fillImages[i].transform.rotation = transformRotation;
            elapsedFill += _currentFillAmounts[i];
        }
    }

    public void UpdateValues(float[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            fillAmountTargets[index] = value;
        }
    }

    public void SetIsActive(bool state)
    {
        _currentPositionTarget = state ? _shownPosition : _hiddenPosition;
    }
}