using Anywhen;
using UnityEngine;

namespace Samples.Scripts
{
    public class SampleNoteOffTester : MonoBehaviour
    {
        public AnywhenInstrument instrument;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                AnywhenRuntime.EventFunnel.HandleNoteEvent(new NoteEvent(0, NoteEvent.EventTypes.NoteOff), instrument,
                    AnywhenMetronome.TickRate.None);

                AnywhenRuntime.EventFunnel.HandleNoteEvent(new NoteEvent(Random.Range(0, 2)), instrument);
            }
        }
    }
}