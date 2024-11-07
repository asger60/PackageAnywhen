using System.Linq;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;

public class InstrumentDatabase : MonoBehaviour
{
    [SerializeField] private AnywhenInstrument[] instruments;


    public AnywhenInstrument GetInstrumentOfType(AnysongTrack.AnyTrackTypes type)
    {
        instruments = ShuffleArray(instruments);

        for (var i = 0; i < instruments.Length; i++)
        {
            var instrument = instruments[i];
            if (instrument.InstrumentType == type)
            {
                return instrument;
            }
        }

        print("returning null");

        return null;
    }

    static T[] ShuffleArray<T>(T[] array)
    {
        System.Random random = new System.Random();
        return array.OrderBy(x => random.Next()).ToArray();
    }


    void UnlinkNoteCLips()
    {
        foreach (var instrument in instruments)
        {
            if (instrument is AnywhenSampleInstrument sampleInstrument)
            {
                sampleInstrument.UnlinkClips();
            }
        }
    }
}