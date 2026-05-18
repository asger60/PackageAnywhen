using System;
using System.Collections.Generic;
using System.Linq;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Unity.Collections;
using UnityEditor;
using UnityEngine;


namespace Anywhen
{
    public class InstrumentDatabase : MonoBehaviour
    {
        [SerializeField] private AnywhenInstrument[] instruments;


        [Serializable]
        public struct LoadedInstrument : IEquatable<LoadedInstrument>
        {
            public AnywhenSampleInstrument Instrument;
            public List<AnywhenNoteClip> clips;

            public struct Unmanaged : IEquatable<Unmanaged>, IDisposable
            {
                public AnywhenSampleInstrument.Unmanaged instrument;
                public NativeArray<AnywhenNoteClip.Unmanaged> clips;

                public bool Equals(Unmanaged other)
                {
                    return instrument.Equals(other.instrument);
                }

                public void Dispose()
                {
                    if (clips.IsCreated) clips.Dispose();
                }
            }

            public Unmanaged ToUnmanaged(Allocator allocator)
            {
                var unmanagedClips = new NativeArray<AnywhenNoteClip.Unmanaged>(clips.Count, allocator);
                for (int i = 0; i < clips.Count; i++)
                {
                    unmanagedClips[i] = clips[i].ToUnmanaged();
                }

                return new Unmanaged
                {
                    instrument = Instrument.ToUnmanaged(),
                    clips = unmanagedClips
                };
            }

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

        public List<LoadedInstrument> LoadedInstruments = new();

        static NativeArray<LoadedInstrument.Unmanaged> _loadedInstruments;

        public static NativeArray<LoadedInstrument.Unmanaged> GetLoadedInstrumentsUnmanaged()
        {
            if (_loadedInstruments.IsCreated && _loadedInstruments.Length == AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count)
            {
                return _loadedInstruments;
            }

            if (_loadedInstruments.IsCreated)
            {
                for (int i = 0; i < _loadedInstruments.Length; i++)
                {
                    _loadedInstruments[i].Dispose();
                }

                _loadedInstruments.Dispose();
            }

            _loadedInstruments = new NativeArray<LoadedInstrument.Unmanaged>(AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count, Allocator.Persistent);
            for (var index = 0; index < AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count; index++)
            {
                _loadedInstruments[index] = AnywhenRuntime.InstrumentDatabase.LoadedInstruments[index].ToUnmanaged(Allocator.Persistent);
            }

            return _loadedInstruments;
        }

        private void OnDestroy()
        {
            if (_loadedInstruments.IsCreated)
            {
                for (int i = 0; i < _loadedInstruments.Length; i++)
                {
                    _loadedInstruments[i].Dispose();
                }

                _loadedInstruments.Dispose();
            }
        }


#if UNITY_EDITOR


        public static void LoadInstrumentNotes(AnywhenSampleInstrument instrument)
        {
            if (!instrument) return;
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
            var loadedInstruments = GetLoadedInstrumentsUnmanaged();
            for (var i = 0; i < loadedInstruments.Length; i++)
            {
                var loaded = loadedInstruments[i];
                var unmanaged = loaded.instrument;
                unmanaged.seed = instrument.seed; // Ignore seed for comparison
                if (unmanaged.Equals(instrument))
                {
                    return loaded.clips;
                }
            }


            Debug.LogError("No instruments loaded in InstrumentDatabase!");
            return default;
        }

        public static void LoadAllInstruments(AnysongObject currentSong)
        {
            foreach (var songTrack in currentSong.Tracks)
            {
                foreach (var audioSource in songTrack.AudioSources)
                {
                    if (audioSource.audioSourceType == AudioSourceSettings.AudioSourceTypes.Sample)
                    {
                        var sampleInstrument = audioSource.sampleSourceSettings.sampleInstrument;
                        if (sampleInstrument && !IsLoaded(sampleInstrument))
                        {
                            LoadInstrumentNotes(sampleInstrument);
                        }
                    }
                }
            }
        }
    }
}