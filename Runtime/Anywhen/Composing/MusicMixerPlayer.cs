using System;
using System.Collections.Generic;
using Anywhen;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class MusicMixerPlayer : MonoBehaviour
{
   


    [Serializable]
    public class MusicModulePlayer
    {
        private AnywhenInstrument[] _instruments;
        private MusicMixerPlayer _musicMixerPlayer;
        private AnySection _anySection;
        private AudioMixerGroup _mixerGroup;
        private bool _isRunning;
        private bool _loaded;

        public void Load(MusicMixerPlayer musicMixerPlayer, AnySection anySection)
        {
            _loaded = true;
            _anySection = anySection;
            _musicMixerPlayer = musicMixerPlayer;
            //_instruments = anySection.instruments;

            _lastTrackNote = new NoteEvent[_instruments.Length];
            for (var i = 0; i < _lastTrackNote.Length; i++)
            {
                _lastTrackNote[i] = new NoteEvent(0, NoteEvent.EventTypes.NoteOn);
            }
        }

        private NoteEvent[] _lastTrackNote;

        public void OnTick16()
        {
            if (!_isRunning) return;

            int step = AnywhenRuntime.Metronome.Sub16;


            for (int track = 0; track < _instruments.Length; track++)
            {
                var thisStep = GetStep(step, track);
                if (thisStep.noteOn)
                {
                    _lastTrackNote[track].state = NoteEvent.EventTypes.NoteOff;
                    AnywhenRuntime.EventFunnel.HandleNoteEvent(_lastTrackNote[track], _instruments[track],
                        AnywhenMetronome.TickRate.Sub16, _mixerGroup);


                    _lastTrackNote[track] = new NoteEvent(NoteEvent.EventTypes.NoteOn, thisStep.offset,
                        thisStep.GetNotes(),
                        new double[] { 0, 0, 0 }, thisStep.expression, 1,
                        thisStep.velocity * _anySection.volume * _instruments[track].volume)
                    {
                        duration = thisStep.duration
                    };


                    AnywhenRuntime.EventFunnel.HandleNoteEvent(_lastTrackNote[track], _instruments[track],
                        AnywhenMetronome.TickRate.Sub16, _mixerGroup);
                }

                if (thisStep.noteOff)
                {
                    _lastTrackNote[track].state = NoteEvent.EventTypes.NoteOff;
                    AnywhenRuntime.EventFunnel.HandleNoteEvent(_lastTrackNote[track], _instruments[track],
                        AnywhenMetronome.TickRate.Sub16, _mixerGroup);
                }
            }
        }

        
        AnySection.AnyPatternStep GetStep(int sub16, int track)
        {
            AnySection.AnyPatternStep step = new AnySection.AnyPatternStep();
            float bestDistance = float.MaxValue;
            //for (var i = 0; i < _musicVariations.Count; i++)
            //{
            //    //var musicVariation = _musicVariations[i];
            //    var pattern = musicVariation.GetPattern(AnywhenMetronome.Instance.CurrentBar);
//
            //    float selectorDistance = Mathf.Abs(
            //        ((0.5f - pattern.tracks[track].steps[sub16].mixWeight) + i) -
            //        _musicMixerPlayer.GetVariationValue());
//
//
            //    if (selectorDistance > 1) continue;
            //    if (selectorDistance < bestDistance)
            //    {
            //        bestDistance = selectorDistance;
            //        step = pattern.tracks[track].steps[sub16];
            //    }
            //}

            return step;
        }


        public void Release()
        {
            _loaded = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
        }


        public void Stop()
        {
            _isRunning = false;
            AnywhenRuntime.Metronome.OnTick16 -= OnTick16;
        }

        public void Play()
        {
            if (_isRunning) return;
            if (!_loaded) return;
            _isRunning = true;
            AnywhenRuntime.Metronome.OnTick16 += OnTick16;
        }
    }

    [FormerlySerializedAs("musicTrack")] public MusicModulePlayer musicModulePlayer;
    private AnySection _currentMusicModule;


    public AnySection testModule1;

    private void SetMusicModule(AnySection anySection)
    {
        if (anySection == null) return;
        musicModulePlayer.Release();
        _currentMusicModule = anySection;
        musicModulePlayer.Load(this, anySection);
    }


    float GetVariationValue()
    {
        return 0;
    }


    public void LoadModule(AnySection moduleObject)
    {
        SetMusicModule(moduleObject);
    }


    public void Play()
    {
        musicModulePlayer.Play();
    }
}