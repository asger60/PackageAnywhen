using System;
using System.Collections;
using System.Collections.Generic;
using Anywhen;
using UnityEngine;

public class ProgressionHandler : MonoBehaviour
{
    public static ProgressionHandler Instance => _instance;
    private static ProgressionHandler _instance;
    public AnywhenProgressionPatternObject[] progressions;
    private void Awake()
    {
        _instance = this;
    }

    public void SetProgressionIndex(int index)
    {
        AnywhenConductor.Instance.OverridePattern(progressions[index]);
    }
}
