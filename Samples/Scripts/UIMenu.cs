
using UnityEngine;
using UnityEngine.UI;

public class UIMenu : MonoBehaviour
{
    public Button playButton;

    private void Start()
    {
        playButton.onClick.AddListener(() =>
        {
            AppHandler.Instance.SetAppState(AppHandler.AppStates.Playing);
        });
    }

    public void SetIsActive(bool state)
    {
        gameObject.SetActive(state);
    }
}
