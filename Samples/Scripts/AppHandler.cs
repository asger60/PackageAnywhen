using System;
using PackageAnywhen.Samples.Scripts;
using UnityEngine;

public class AppHandler : MonoBehaviour
{
    public enum AppStates
    {
        None,
        Menu,
        Playing,
        Welcome
    }

    private AppStates _currentAppState;
    public GodHand godHand;
    public UIMenu uiMenu;
    public static AppHandler Instance => _instance;
    private static AppHandler _instance;
    public GameObject menuButton;
    public UITrackSelector trackSelector;
    public CameraHandler cameraHandler;
    public GameObject welcomePanel;
    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        SetAppState(AppStates.Welcome);
    }

    public void SetAppState(AppStates newAppState)
    {
        switch (newAppState)
        {
            case AppStates.None:
                break;
            case AppStates.Menu:
                welcomePanel.SetActive(false);
                godHand.SetIsActive(false);
                uiMenu.SetIsActive(true);
                TrackHandler.Instance.HideTrackInterfaces();
                menuButton.SetActive(false);
                trackSelector.SetIsActive(false);
                cameraHandler.SetIsInGame(false);
                break;
            case AppStates.Playing:
                welcomePanel.SetActive(false);
                godHand.SetIsActive(true);
                uiMenu.SetIsActive(false);
                TrackHandler.Instance.ShowTrackInterfaces();
                menuButton.SetActive(true);
                trackSelector.SetIsActive(true);
                cameraHandler.SetIsInGame(true);

                break;
            case AppStates.Welcome:
                godHand.SetIsActive(false);
                welcomePanel.SetActive(true);
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
            case AppStates.Welcome:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void StartGame()
    {
        SetAppState(AppStates.Playing);
    }
}