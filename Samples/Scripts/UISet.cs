using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class UISet : MonoBehaviour
{
    [FormerlySerializedAs("selector")] public FillSelector fillSelector;
    public MixView mixView;


    private void Start()
    {
        gameObject.SetActive(true);
    }

    public void SetIsActive(bool state)
    {
        mixView.SetIsActive(state);
        fillSelector.SetIsActive(state);
        
    }
}
