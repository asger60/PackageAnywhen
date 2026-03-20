using System;
using UnityEngine;

[Serializable]
public struct DynamicRangeFloat
{
    public float value;
    public float max;

    public DynamicRangeFloat(float value, float max)
    {
        this.value = value;
        this.max = max;
    }
}

public class DynamicRangeAttribute : PropertyAttribute
{
}