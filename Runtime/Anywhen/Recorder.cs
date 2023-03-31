using System;
using System.Collections.Generic;
using PackageFileHandler.Runtime;
using UnityEngine;

namespace Anywhen
{
    public class Recorder : MonoBehaviour
    {
        public static Recorder Instance => _instance;
        public JamModeBase[] jamObjects;

        [NonReorderable] public List<Recording> recordings16;

        private static Recorder _instance;

        private bool _isActive = false;
        private RecorderJamSave _loadedRecorderData, _saveRecorderData;

        public enum Mode
        {
            Record,
            Stopped,
            Playback
        }

        private void Awake()
        {
            _instance = this;
        }

        public Recording PrepareRecorder(JamModeBase jam)
        {
            //LoadRecording();
            //int jamObjectIndex = GetIndexFromSettingsObject(jamObject);
           // foreach (var recording in recordings16)
           // {
           //     recording.ResetTimers();
           // }
//
           // if (recordings16.Count >= 0)
           // {
           //     foreach (var recording in recordings16)
           //     {
           //         if (recording.jamObjectIndex == jamObjectIndex)
           //         {
           //             if (recording.GetLength() != 0)
           //                 return recording;
           //         }
           //     }
           // }
//
           // print("#Recorder#did not find any saved recordings");
           // recordings16.Add(new Recording(jamObjectIndex, jamObject.loopLength, jamObject.quantization));
//
           // return recordings16[^1];
           return null;
        }


        private void LoadRecording(string recordingID)
        {
            recordings16.Clear();
            _loadedRecorderData = new RecorderJamSave();
            //todo fix me
            //_loadedRecorderData = FileHandler.Load(recordingID, _loadedRecorderData);

            if (_loadedRecorderData.recordings != null)
            {
                print("#Recorder#Loading saved recording for " + recordingID);
                recordings16.AddRange(_loadedRecorderData.recordings);
            }

            foreach (var recording in recordings16)
            {
                recording.ResetTimers();
            }
        }

        public void SetActive(bool state)
        {
            if (state == _isActive) return;

            if (state)
            {
                AnywhenMetronome.Instance.OnTick16 += OnTick16;
            }
            else
            {
                foreach (var recording in recordings16)
                {
                    recording.AllNotesOff();
                }

                recordings16.Clear();
                AnywhenMetronome.Instance.OnTick16 -= OnTick16;
            }

            _isActive = state;
        }

        struct QueuedEvent
        {
            public Recording Recorder;
            public NoteEvent NewNoteEvent;

            public QueuedEvent(NoteEvent newNoteEvent, Recording recorder)
            {
                Recorder = recorder;
                NewNoteEvent = newNoteEvent;
            }
        }

        private List<QueuedEvent> _recordingQueue = new List<QueuedEvent>();

        public void RecordNoteEvent(NoteEvent e, Recording recording)
        {
            _recordingQueue.Add(new QueuedEvent(e, recording));
        }


        public void SaveRecording(string recordingID)
        {
            print("#Recorder#saving recording");
            for (var i = recordings16.Count - 1; i >= 0; i--)
            {
                var recording = recordings16[i];
                if (recording._loopLength == 0)
                    recordings16.RemoveAt(i);
            }

            _saveRecorderData = new RecorderJamSave
            {
                recordings = recordings16.ToArray()
            };
            
            FileHandler.Save(recordingID, _saveRecorderData);
        }

        [ContextMenu("delete recording")]
        public void DeleteRecording(string recordingID)
        {
            if (_loadedRecorderData != null)
            {
                FileHandler.Delete(recordingID);
                _loadedRecorderData = null;
            }
            
        }

        public bool IsStopped => _isStopped;
        private bool _isStopped;

        private void OnTick16()
        {
            if (_isStopped) return;
            foreach (var recording in recordings16)
            {
                recording.Tick16(AnywhenMetronome.Instance.Sub16);
            }

            foreach (var queuedEvent in _recordingQueue)
            {
                queuedEvent.Recorder.RecordNoteEvent(queuedEvent.NewNoteEvent);
            }

            _recordingQueue.Clear();
        }



     


        [Serializable]
        public class Recording
        {
            public List<NoteEvent> events = new List<NoteEvent>();
            public Mode recorderMode;
            public int jamObjectIndex;

            private int _currentBar = -1;
            public int _loopLength;
            public float _quantization;

            private List<NoteEvent> _stepEvents = new List<NoteEvent>();

            public NoteEvent RecordNoteEvent(NoteEvent e)
            {
                events.Add(e);
                return e;
            }


            public List<NoteEvent> GetEvents(int step)
            {
                _stepEvents ??= new List<NoteEvent>();
                _stepEvents.Clear();

                //todo - fix me
                //foreach (var e in events)
                //{
                //    if (e.step == step) _stepEvents.Add(e);
                //}

                return _stepEvents;
            }


            public void SetInternalStep(int step)
            {
                _internalStep = step;
            }

            public Recording(int jamObjectIndex, int loopLength, float quantization)
            {
                this.jamObjectIndex = jamObjectIndex;
                _loopLength = loopLength;
                _quantization = quantization;
                recorderMode = Mode.Record;
            }

            private int _internalStep;
            public int InternalStep => _internalStep;

            public void Tick16(int step)
            {
                if (step == 0) _currentBar++;
                _internalStep++;
                if (_currentBar == _loopLength)
                {
                    _internalStep = 0;
                    _currentBar = 0;
                }

                if (recorderMode != Mode.Stopped)
                {
                    var stepEvents = GetEvents(_internalStep);
                    foreach (var e in stepEvents)
                    {
                        var stepEvent = e;
                        stepEvent.drift = Mathf.Lerp((float)stepEvent.drift, 0, _quantization);
                        //todo - fix me
                        //stepEvent.ScheduledPlaytime = Metronome.Instance.GetScheduledPlaytime(Metronome.TickRate.Sub16);
                        
                        //EventFunnel.Instance.HandleNoteEvent(stepEvent, Instance.jamObjects[jamObjectIndex], false,
                        //    AudioManager.Instance.jamMixerGroup);
                    }
                }
            }


            public int GetCurrentBar()
            {   
                return _currentBar;
            }

            public void ClearEvents()
            {
                AllNotesOff();
                events?.Clear();
            }

            public void ClearEvents(int step)
            {
                foreach (var e in GetEvents(step))
                {
                    events.Remove(e);
                }
            }

            public int GetLength()
            {
                return _loopLength;
            }

            public AnywhenMetronome.StepTiming GetStepTiming()
            {
                float nextTickDrift = 0;
                int step = _internalStep;

                var timeToNextPlay = AnywhenMetronome.Instance.GetTimeToNextPlay(AnywhenMetronome.TickRate.Sub16);
                if (timeToNextPlay < AnywhenMetronome.Instance.GetLength(AnywhenMetronome.TickRate.Sub16) / 2f)
                {
                    step += 1;
                }

                step = (int)Mathf.Repeat(step, 16 * _loopLength);

                return new AnywhenMetronome.StepTiming(step, nextTickDrift);
            }

            //public void TickBars(int step)
            //{
            //    step = (int)Mathf.Repeat(step, 16);
            //    if (step == 0) _currentBar++;
            //    _currentBar = (int)Mathf.Repeat(_currentBar, _loopLength);
            //}

            public void AllNotesOff()
            {
                for (int i = 0; i < 128; i++)
                {
                    //todo - fix me
                    //EventFunnel.HandleNoteEvent(
                    //    new NoteEvent(0, i, 0, 0, Vector2.zero, 0, NoteEvent.EventTypes.NoteOff),
                    //    Instance.jamObjects[jamObjectIndex], true);
                }
            }

            public void ResetTimers()
            {
                _currentBar = -1;
                _internalStep = -1;
            }
        }


        [Serializable]
        public class RecorderJamSave : FileHandler.SaveData
        {
            
            [NonReorderable] public Recording[] recordings;
        }

        public void Reset()
        {
            foreach (var recording in recordings16)
            {
                recording.ResetTimers();
            }
        }

        public void Play()
        {
            _isStopped = false;
        }

        public void Stop()
        {
            _isStopped = true;
        }

        public void SetStep(int step)
        {
            foreach (var recording in recordings16)
            {
                recording.SetInternalStep(step);
            }
        }
    }
}