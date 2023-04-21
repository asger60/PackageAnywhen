using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppHandler : MonoBehaviour
{
    public enum AppStates
    {
        None,
        Menu,
        Playing
    }

    private AppStates _currentAppState;
    public GodHand godHand;
    public UIMenu uiMenu;
    public static AppHandler Instance => _instance;
    private static AppHandler _instance;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        SetAppState(AppStates.Menu);
    }

    public void SetAppState(AppStates newAppState)
    {
        switch (newAppState)
        {
            case AppStates.None:
                break;
            case AppStates.Menu:
                godHand.SetIsActive(false);
                uiMenu.SetIsActive(true);
                break;
            case AppStates.Playing:
                godHand.SetIsActive(true);
                uiMenu.SetIsActive(false);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newAppState), newAppState, null);
        }

        _currentAppState = newAppState;
    }

    private void Update()
    {
        switch (_currentAppState)
        {
            case AppStates.None:
                break;
            case AppStates.Menu:
                break;
            case AppStates.Playing:
                if (Input.GetKeyUp(KeyCode.Escape))
                {
                    SetAppState(AppStates.Menu);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}