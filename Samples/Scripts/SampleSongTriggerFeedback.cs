using Anywhen;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class SampleSongTriggerFeedback : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI triggerTypeText, songNameText;
    private AnywhenTrigger _anywhenTrigger;
    private AnywhenPlayer _anywhenPlayer;
    

    
    void Update()
    {
        if (!_anywhenPlayer) _anywhenPlayer = GetComponentInParent<AnywhenPlayer>();
        if (!_anywhenTrigger) _anywhenTrigger = GetComponentInParent<AnywhenTrigger>();
        
        triggerTypeText.SetText(_anywhenTrigger.triggerType.ToString());
        
        songNameText.SetText(_anywhenPlayer.AnysongObject ? _anywhenPlayer.AnysongObject.name : "no song selected");

    }
}
