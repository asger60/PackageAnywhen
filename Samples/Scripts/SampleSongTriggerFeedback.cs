using Anywhen;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class SampleSongTriggerFeedback : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI triggerTypeText, songNameText;
    private AnywhenTrigger _anywhenTrigger;
    private AnysongPlayer _anysongPlayer;
    

    
    void Update()
    {
        if (!_anysongPlayer) _anysongPlayer = GetComponentInParent<AnysongPlayer>();
        if (!_anywhenTrigger) _anywhenTrigger = GetComponentInParent<AnywhenTrigger>();
        
        triggerTypeText.SetText(_anywhenTrigger.triggerType.ToString());
        
        songNameText.SetText(_anysongPlayer.AnysongObject ? _anysongPlayer.AnysongObject.name : "no song selected");

    }
}
