using Anywhen.SettingsObjects;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NoteClipPlayer : MonoBehaviour
{
    private double _scheduledPlayTime, _scheduledStopTime;
    private bool _scheduledPlay;
    private bool _isPlaying;
    private int _sampleIndex;
    private bool _noteDown;
    private AnywhenNoteClip _noteClip;

    public AnywhenNoteClip testClip;

    [ContextMenu("Test")]
    void Test()
    {
        PlayScheduled(0, testClip);
    }

    public void PlayScheduled(double absoluteTime, AnywhenNoteClip clip)
    {
        AudioClip myClip = AudioClip.Create("MySound", 1024, 2, 44100, false);
        var audioSource = GetComponent<AudioSource>();
        audioSource.clip = myClip;
        audioSource.Play();
        print("play");
        _noteClip = clip;
        _scheduledPlay = true;
        _scheduledPlayTime = absoluteTime;
        _isPlaying = false;
        print(AudioSettings.outputSampleRate);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (_noteClip == null)
        {
            return;
        }

        if (!_isPlaying && _scheduledPlay && _scheduledPlayTime >= 0 && AudioSettings.dspTime >= _scheduledPlayTime)
        {
            _isPlaying = true;
            _sampleIndex = 0;
            _scheduledPlay = false;
            _noteDown = true;
        }

        if (!_isPlaying) return;

        if (_scheduledStopTime > 0 && AudioSettings.dspTime > _scheduledStopTime)
        {
            _noteDown = false;
            _scheduledStopTime = -1;
        }

        for (int i = 0; i < data.Length; i += _noteClip.channels)
        {
            for (int channel = 0; channel < _noteClip.channels; channel++)
            {
                var sourceSample1 = Mathf.Max(Mathf.Min(_sampleIndex + i + channel, _noteClip.clipSamples.Length - 1),
                    0);
                
                
                data[i + channel] = _noteClip.clipSamples[sourceSample1];
            }
        }
        
        
        //if (_isPlaying && _noteDown && _sampleIndex > _noteClip.loopStart)
        //{
        //    _sampleIndex -= _noteClip.loopLength;
        //}
        //else
        {
            _sampleIndex += data.Length;
        }
        if (_sampleIndex >= _noteClip.clipSamples.Length - 1)
        {
            _isPlaying = false;
        }
    }
}