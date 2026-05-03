using System;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;


public struct AnywhenAudioVoice
{
    private double _scheduledStartTime, _scheduledEndTime;
    private AnywhenNoteClip.Unmanaged _clipData;
    private int _sampleCount;
    private double _samplePosition;
    private bool _noteOn;
    private bool _noteQueued;
    private float _pitch;
    private float _velocity;
    AudioProcessorEnvelope _voiceEnvelope;
    private AnywhenSampleInstrument.Unmanaged _sampleInstrument;
    private AudioProcessorSettings.EnvelopeSettings.Unmanaged _envelopeSettings;
    private AnysongTrackSettings.AudioSourceType _audioSourceType;
    private AnysongTrackSettings.SynthOscillatorTypes _oscillatorType;

    public void Init(int sampleRate)
    {
        _voiceEnvelope = new AudioProcessorEnvelope(sampleRate);
    }

    public void UpdateVoiceSettings(
        AnywhenSampleInstrument.Unmanaged sampleInstrument,
        AudioProcessorSettings.EnvelopeSettings.Unmanaged envelopeSettings,
        AnysongTrackSettings.AudioSourceType audioSourceType,
        AnysongTrackSettings.SynthOscillatorTypes oscillatorType)
    {
        _sampleInstrument = sampleInstrument;
        _envelopeSettings = envelopeSettings;
        _audioSourceType = audioSourceType;
        _oscillatorType = oscillatorType;
    }

    internal float Process(double dspTime, in AnysongTrack anysongTrack)
    {
        float clipAmplitude = 0;
        if (_noteQueued && dspTime >= _scheduledStartTime)
        {
            switch (_audioSourceType)
            {
                case AnysongTrackSettings.AudioSourceType.Sample:
                    int channels = _clipData.channels;
                    int frameCount = channels > 0 ? _sampleCount / channels : 0;

                    if (_clipData.clipSamples.IsCreated && _samplePosition < frameCount)
                    {
                        int index = (int)_samplePosition;
                        float t = (float)(_samplePosition - index);

                        float frameAmplitude = 0;
                        bool canInterpolate = index < frameCount - 1;

                        for (int c = 0; c < channels; c++)
                        {
                            float s0 = _clipData.clipSamples[index * channels + c];
                            if (canInterpolate)
                            {
                                float s1 = _clipData.clipSamples[(index + 1) * channels + c];
                                frameAmplitude += s0 + t * (s1 - s0);
                            }
                            else
                            {
                                frameAmplitude += s0;
                            }
                        }

                        if (channels > 1)
                            frameAmplitude /= channels;

                        clipAmplitude = frameAmplitude;
                        _samplePosition += _pitch;
                    }
                    else
                    {
                        _noteQueued = false;
                    }

                    break;
                case AnysongTrackSettings.AudioSourceType.Synth:
                    switch (_oscillatorType)
                    {
                        case AnysongTrackSettings.SynthOscillatorTypes.Sine:

                            break;
                        case AnysongTrackSettings.SynthOscillatorTypes.Saw:
                            break;
                        case AnysongTrackSettings.SynthOscillatorTypes.Square:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _voiceEnvelope.SetGate(dspTime >= _scheduledStartTime && dspTime <= _scheduledEndTime);

        return clipAmplitude * _velocity * _voiceEnvelope.Process((float)dspTime, anysongTrack);
    }

    internal void QueueNote(AnywhenAudioGenerator.PlaybackEvent playbackEvent)
    {
        _voiceEnvelope.SetSettings(_envelopeSettings);
        _voiceEnvelope.Reset();
        _noteQueued = true;
        _scheduledStartTime = playbackEvent.ScheduledPlayTime;
        _scheduledEndTime = playbackEvent.ScheduledEndTime;
        var clipSettings = _sampleInstrument.GetNoteClipSettings(playbackEvent.Note.noteIndex);
        _clipData = clipSettings.NoteClipUnmanaged;
        _pitch = clipSettings.clipPitch;
        _sampleCount = _clipData.clipSamples.IsCreated ? _clipData.clipSamples.Length : 0;
        _samplePosition = 0;
        _velocity = playbackEvent.Note.velocity;
    }

    public bool IsIdle => !_noteQueued;
    public double ScheduledStartTime => _scheduledStartTime;
}