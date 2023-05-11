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

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                SetKeyState(0, true);
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                SetKeyState(0, false);
            }
        }

        void SetKeyState(int keyIndex, bool state)
        {
            print("set state " + state);
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

            NoteEvent e = new NoteEvent(_noteIndex, state ? NoteEvent.EventTypes.NoteOn : NoteEvent.EventTypes.NoteOff);
        
            EventFunnel.HandleNoteEvent(e, anywhenInstrument, quantization);
        }
    }
}