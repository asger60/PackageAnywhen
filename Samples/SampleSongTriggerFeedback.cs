using Anywhen;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class SampleSongTriggerFeedback : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI triggerTypeText, songNameText;
    private AnywhenSongTrigger _anywhenSongTrigger;
    private AnysongPlayer _anysongPlayer;
    

    
    void Update()
    {
        if (!_anysongPlayer) _anysongPlayer = GetComponentInParent<AnysongPlayer>();
        if (!_anywhenSongTrigger) _anywhenSongTrigger = GetComponentInParent<AnywhenSongTrigger>();
        
        triggerTypeText.SetText(_anywhenSongTrigger.triggerType.ToString());
        
        songNameText.SetText(_anysongPlayer.AnysongObject ? _anysongPlayer.AnysongObject.name : "no song selected");

    }
}
