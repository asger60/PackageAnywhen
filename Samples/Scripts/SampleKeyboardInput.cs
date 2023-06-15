using System;
using Anywhen;
using Anywhen.SettingsObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Samples.Scripts
{
    public class SampleKeyboardInput : MonoBehaviour
    {
        public AnywhenInstrument anywhenInstrument;
        public AnywhenMetronome.TickRate quantization;
        int _noteIndex = 0;

        public enum PlayMode
        {
            Random,
            SequenceUp,
            SequenceDown
        }

        public PlayMode playMode;

        private NoteEvent _e;

        private void Start()
        {
            _e = new NoteEvent(0, NoteEvent.EventTypes.NoteDown)
            {
                expression1 = 1
            };
        }

        private bool _noteDown;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetKeyState(0, true);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetKeyState(0, false);
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                SetKeyState(0, true);
                _noteDown = true;
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                SetKeyState(0, false);
                _noteDown = false;
            }

            //if (_noteDown)
            //{
            //    _e.expression1 = Mathf.MoveTowards(_e.expression1, 1.5f, Time.deltaTime * 0.5f);
            //    AnywhenRuntime.EventFunnel.HandleNoteEvent(_e, anywhenInstrument, quantization);
            //}
        }

        void SetKeyState(int keyIndex, bool state)
        {
            if (state)
            {
                switch (playMode)
                {
                    case PlayMode.Random:
                        _noteIndex = Random.Range(0, 5);
                        break;
                    case PlayMode.SequenceUp:
                        _noteIndex++;
                        _noteIndex = (int)Mathf.Repeat(_noteIndex, 5);

                        break;
                    case PlayMode.SequenceDown:
                        _noteIndex--;
                        _noteIndex = (int)Mathf.Repeat(_noteIndex, 5);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (state)
            {
                _e.expression1 = 1;
            }

            NoteEvent e = new NoteEvent(0,
                state ? NoteEvent.EventTypes.NoteOn : NoteEvent.EventTypes.NoteOff);


            AnywhenRuntime.EventFunnel.HandleNoteEvent(e, anywhenInstrument, quantization);
        }
    }
}