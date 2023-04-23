using Anywhen;
using UnityEngine;
using UnityEngine.UI;

public class UIMenu : MonoBehaviour
{
    public Button playButton;
    public UIToggleGroup tempoToggle;
    public UIToggleGroup progressionToggle;
    private int[] _presetTempis = new[] { 90, 120, 130 };

    private void Start()
    {
        playButton.onClick.AddListener(() => { AppHandler.Instance.SetAppState(AppHandler.AppStates.Playing); });
        tempoToggle.OnSelect = OnTempoSelect;
        progressionToggle.OnSelect = OnProgressionSelect;
    }

    private void OnProgressionSelect(int index)
    {
        ProgressionHandler.Instance.SetProgressionIndex(index);
    }

    private void OnTempoSelect(int index)
    {
        AnywhenMetronome.Instance.SetTempo(_presetTempis[index]);
    }

    public void SetIsActive(bool state)
    {
        gameObject.SetActive(state);
    }
}