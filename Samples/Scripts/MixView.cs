using Samples.Scripts;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MixView : MonoBehaviour
{
    public Image[] fillImages;

    [FormerlySerializedAs("fillAmounts")] [Range(0, 1f)] public float[] fillAmountTargets;
    private float[] _currentFillAmounts = new float[4];
    
    
    private void Update()
    {
        float elapsedFill = 0;
        for (var i = 0; i < fillImages.Length; i++)
        {
            _currentFillAmounts[i] = Mathf.Lerp(_currentFillAmounts[i], fillAmountTargets[i], Time.deltaTime*5);
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
}