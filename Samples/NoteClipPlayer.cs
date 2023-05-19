using System;
using Anywhen.SettingsObjects;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NoteClipPlayer : MonoBehaviour
{
    private double _scheduledPlayTime, _scheduledStopTime;
    private bool _scheduledPlay;
    private bool _isPlaying;
    private double _samplePos;
    private int _realSampleIndex;
    private bool _noteDown;
    private AnywhenNoteClip _noteClip;

    public AnywhenNoteClip testClip;
    private int _sampleRate;
    private double _sampleStepFrac;

    [ContextMenu("Test")]
    void Test()
    {
        PlayScheduled(0, testClip);
    }

    public void PlayScheduled(double absoluteTime, AnywhenNoteClip clip)
    {
        AudioClip myClip = AudioClip.Create("MySound", 1024, 2, 48000, false);
        var audioSource = GetComponent<AudioSource>();
        audioSource.clip = myClip;
        audioSource.Play();
        _noteClip = clip;
        _scheduledPlay = true;
        _scheduledPlayTime = absoluteTime;
        _isPlaying = false;
        _sampleRate = AudioSettings.outputSampleRate;
        _sampleStepFrac = _noteClip.frequency / (float)_sampleRate;
        print(_sampleStepFrac);
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
            _samplePos = 0;
            _scheduledPlay = false;
            _noteDown = true;
        }

        if (!_isPlaying) return;

        if (_scheduledStopTime > 0 && AudioSettings.dspTime > _scheduledStopTime)
        {
            _noteDown = false;
            _scheduledStopTime = -1;
        }


        int i = 0;
        while (i < data.Length)
        {
            for (int channel = 0; channel < _noteClip.channels; channel++)
            {
                int sampleIndex = (int)_samplePos;
                double f = (double)_samplePos - sampleIndex;
                var sourceSample1 = Mathf.Min((sampleIndex), _noteClip.clipSamples.Length - 1);
                var sourceSample2 = Mathf.Min((sampleIndex) + 1, _noteClip.clipSamples.Length - 1);
              
                double e = ((1 - f) * _noteClip.clipSamples[sourceSample1]) + (f * _noteClip.clipSamples[sourceSample2]);
                data[i] = (float)e;
                //data[i] = Mathf.Lerp(_noteClip.clipSamples[sourceSample1], _noteClip.clipSamples[sourceSample2],
                //    (float)f);

                _samplePos += _sampleStepFrac/2f;
                i++;
            }
            //data[i] =
            //    ((1 - f) * _noteClip.clipSamples[sourceSample1]) + (f * _noteClip.clipSamples[sourceSample2]);
            //for (int channel = 0; channel < _noteClip.channels; channel++)
            //{
            //    var sourceSample1 = Mathf.Min(i + channel + SI_floor, _noteClip.clipSamples.Length - 1);
            //    var sourceSample2 = Mathf.Min(i + channel + SI_floor + _noteClip.channels,
            //        _noteClip.clipSamples.Length - 1);
//
//
            //    data[i + channel] =
            //        ((1 - f) * _noteClip.clipSamples[sourceSample1]) + (f * _noteClip.clipSamples[sourceSample2]);
            //    //data[(i + channel)] = Mathf.Lerp(
            //    //    _noteClip.clipSamples[sourceSample1],
            //    //    _noteClip.clipSamples[sourceSample2],
            //    //    f);
            //}
        }


        //if (_isPlaying && _noteDown && _sampleIndex > _noteClip.loopStart)
        //{
        //    _sampleIndex -= _noteClip.loopLength;
        //}
        //else
        {
            //sampleIndex += data.Length * _sampleStepFrac;
        }
        if (_samplePos >= _noteClip.clipSamples.Length - 1)
        {
            _isPlaying = false;
        }
    }
}