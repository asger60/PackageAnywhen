using Anywhen.Composing;
using UnityEngine;

public class InstrumentDatabase : MonoBehaviour
{
    [SerializeField] private AnywhenInstrument[] instruments;


    public AnywhenInstrument GetInstrumentOfType(AnysongTrack.AnyTrackTypes type)
    {
        print("getting instrument of type " + type);
        int index = Random.Range(0, instruments.Length);
        for (var i = 0; i < instruments.Length; i++)
        {
            index = (int)Mathf.Repeat(index, instruments.Length);
            var instrument = instruments[index];
            if (instrument.InstrumentType == type)
            {
                print("returning " + instrument.name);
                return instrument;
            }

            index++;
        }

        print("returning null");

        return null;
    }
}