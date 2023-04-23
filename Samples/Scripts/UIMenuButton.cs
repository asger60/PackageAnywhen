using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuButton : MonoBehaviour
{
    public Button button;

    private void Start()
    {
        button.onClick.AddListener(() => { AppHandler.Instance.SetAppState(AppHandler.AppStates.Menu); });
    }
}