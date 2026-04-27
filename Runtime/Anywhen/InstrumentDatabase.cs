using System;
using System.Collections.Generic;
using System.Linq;
using Anywhen;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Unity.Collections;
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

        public NativeArray<AnywhenNoteClip.Unmanaged> UnmanagedClips
        {
            get
            {
                var r = new NativeArray<AnywhenNoteClip.Unmanaged>(clips.Count, Allocator.Temp);
                for (int i = 0; i < clips.Count; i++)
                {
                    r[i] = clips[i].ToUnmanaged();
                }

                return r;
            }
        }

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

    //public AnywhenInstrument GetInstrumentOfType(AnysongTrackSettings.AnyTrackTypes type)
    //{
    //    instruments = ShuffleArray(instruments);
//
    //    for (var i = 0; i < instruments.Length; i++)
    //    {
    //        var instrument = instruments[i];
    //        if (instrument.InstrumentType == type)
    //        {
    //            return instrument;
    //        }
    //    }
//
    //    print("returning null");
//
    //    return null;
    //}

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
    public static bool IsLoaded(AnywhenSampleInstrument instrument)
    {
        foreach (var loadedInstrument in AnywhenRuntime.InstrumentDatabase.LoadedInstruments)
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

    public static NativeArray<AnywhenNoteClip.Unmanaged> GetNoteClips(AnywhenSampleInstrument.Unmanaged instrument)
    {
        for (var i = 0; i < AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count; i++)
        {
            var loaded = AnywhenRuntime.InstrumentDatabase.LoadedInstruments[i];
            if (loaded.Instrument != null && loaded.Instrument.ToUnmanaged().Equals(instrument))
            {
                return loaded.UnmanagedClips;
            }
        }

        // If exact match fails, try matching without seed (in case seed was generated differently)
        for (var i = 0; i < AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count; i++)
        {
            var loaded = AnywhenRuntime.InstrumentDatabase.LoadedInstruments[i];
            if (loaded.Instrument != null)
            {
                var unmanaged = loaded.Instrument.ToUnmanaged();
                unmanaged.seed = instrument.seed; // Ignore seed for comparison
                if (unmanaged.Equals(instrument))
                {
                    return loaded.UnmanagedClips;
                }
            }
        }

        if (AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count > 0)
        {
            Debug.LogWarning("No instrument found for unmanaged struct. Falling back to first loaded instrument.");
            return AnywhenRuntime.InstrumentDatabase.LoadedInstruments[0].UnmanagedClips;
        }

        Debug.LogError("No instruments loaded in InstrumentDatabase!");
        return default;
    }
}