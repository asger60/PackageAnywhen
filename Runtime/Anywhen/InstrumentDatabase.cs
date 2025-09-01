using System;
using System.Collections.Generic;
using System.Linq;
using Anywhen;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class InstrumentDatabase : MonoBehaviour
{
    [SerializeField] private AnywhenInstrument[] instruments;


    [Serializable]
    public struct LoadedInstrument : IEquatable<LoadedInstrument>
    {
        public AnywhenSampleInstrument Instrument;
        public List<AnywhenNoteClip> clips;

        public bool Equals(LoadedInstrument other)
        {
            return Equals(Instrument, other.Instrument);
        }

        public void SetClips(List<AnywhenNoteClip> newClips)
        {
            clips.Clear();
            clips.AddRange(newClips);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Instrument, clips);
        }
    }

    public List<LoadedInstrument> LoadedInstruments = new List<LoadedInstrument>();

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

#if UNITY_EDITOR


    public static void LoadInstrumentNotes(AnywhenSampleInstrument instrument)
    {
        //if (AnywhenRuntime.InstrumentDatabase.IsLoaded(instrument)) return;
        var newLoadInstrument = new LoadedInstrument
        {
            Instrument = instrument,
            clips = instrument.LoadClips()
        };
        if (!AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Contains(newLoadInstrument))
        {
            AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Add(newLoadInstrument);
        }
        else
        {
            foreach (var instrumentDatabase in AnywhenRuntime.InstrumentDatabase.LoadedInstruments)
            {
                if (instrumentDatabase.Instrument == instrument)
                {
                    instrumentDatabase.SetClips(instrument.LoadClips());
                }
            }
        }

        EditorUtility.SetDirty(AnywhenRuntime.InstrumentDatabase);
    }
#endif
    bool IsLoaded(AnywhenSampleInstrument instrument)
    {
        foreach (var loadedInstrument in LoadedInstruments)
        {
            if (loadedInstrument.Instrument == instrument) return true;
        }

        return false;
    }

    public static List<AnywhenNoteClip> GetNoteClips(AnywhenSampleInstrument instrument)
    {
        foreach (var loadedInstrument in AnywhenRuntime.InstrumentDatabase.LoadedInstruments)
        {
            if (loadedInstrument.Instrument == instrument)
                return loadedInstrument.clips;
        }

        return null;
    }
}