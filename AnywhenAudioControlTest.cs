using Anywhen;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AnywhenAudioControlTest : MonoBehaviour
{
    private AudioSource _audioSource;
    private float _mPreviousFrequency;
    [SerializeField] AnywhenMetronome metronome;
    [SerializeField] AnywhenNoteClip[] noteClips;
    [SerializeField] [Range(1, 100)] private int voices;
    //[SerializeField] AnysongObject anysongObject;

    void Awake()
    {
        _audioSource = gameObject.GetComponent<AudioSource>();
    }

    private void Start()
    {
        _audioSource.Play();
        //var handle = _audioSource.generatorInstance;

        //if (ControlContext.builtIn.Exists(handle))
        //{
        //    Debug.Log("sending setup message");
        //    ControlContext.builtIn.SendMessage(handle, new AnywhenAudioGenrator.TriggerSetupMsg(anysongObject));
        //}


        metronome.OnTick16 += OnTick16;
    }

    private void OnTick16()
    {
        if (metronome.Sub16 % 4 == 0)
        {
            var noteEvent = new SimpleNoteEvent(0)
            {
                duration = 0.1f
            };
            TriggerNoteClip(noteEvent, metronome.GetScheduledPlaytime(), 0);
        }

        //if (metronome.Sub16 % 2 == 0)
        //{
        //    var noteEvent = new SimpleNoteEvent(39)
        //    {
        //        duration = 0.1f
        //    };
//
        //    TriggerNoteClip(noteEvent, metronome.GetScheduledPlaytime(), 0);
        //}
//
        //if (metronome.Sub16 % 8 == 0)
        //{
        //    var noteEvent = new SimpleNoteEvent(38)
        //    {
        //        duration = 0.1f
        //    };
//
        //    TriggerNoteClip(noteEvent, metronome.GetScheduledPlaytime(), 0);
        //}
//
        ////if (metronome.Sub16 % 8 == 0)
        //{
        //    var noteEvent = new SimpleNoteEvent(42)
        //    {
        //        duration = 0.1f
        //    };
//
        //    TriggerNoteClip(noteEvent, metronome.GetScheduledPlaytime(), 0);
        //}
    }


    void TriggerNoteClip(SimpleNoteEvent noteEvent, double scheduledPlayTime, int trackIndex)
    {
        var handle = _audioSource.generatorInstance;
        if (ControlContext.builtIn.Exists(handle))
        {
            ControlContext.builtIn.SendMessage(
                handle,
                new AnywhenAudioGenrator.TriggerNoteClipMsg(noteEvent, scheduledPlayTime, trackIndex));
        }
    }
}