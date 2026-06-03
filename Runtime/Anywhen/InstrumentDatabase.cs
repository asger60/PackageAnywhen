using System;
using System.Collections.Generic;
using System.Linq;
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using Anywhen.Synth;
using Unity.Burst;
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
                public AnywhenSampleInstrument.Unmanaged Instrument;
                public NativeArray<AnywhenNoteClip.Unmanaged> Clips;

                public bool Equals(Unmanaged other)
                {
                    return Instrument.Equals(other.Instrument);
                }

                public void Dispose()
                {
                    if (Clips.IsCreated) Clips.Dispose();
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
                    Instrument = Instrument.ToUnmanaged(),
                    Clips = unmanagedClips
                };
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

        private struct SharedLoadedInstrumentsKey
        {
        }

        private static readonly SharedStatic<NativeArray<LoadedInstrument.Unmanaged>> LoadedInstrumentsUnmanaged =
            SharedStatic<NativeArray<LoadedInstrument.Unmanaged>>.GetOrCreate<InstrumentDatabase, SharedLoadedInstrumentsKey>();


        public static void RefreshUnamangedInstruments()
        {
            Debug.Log("Refreshing instrument database");
            if (LoadedInstrumentsUnmanaged.Data.IsCreated)
            {
                for (int i = 0; i < LoadedInstrumentsUnmanaged.Data.Length; i++)
                {
                    LoadedInstrumentsUnmanaged.Data[i].Dispose();
                }

                LoadedInstrumentsUnmanaged.Data.Dispose();
            }


            LoadedInstrumentsUnmanaged.Data =
                new NativeArray<LoadedInstrument.Unmanaged>(AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count, Allocator.Persistent);

            for (var index = 0; index < AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count; index++)
            {
                LoadedInstrumentsUnmanaged.Data[index] = AnywhenRuntime.InstrumentDatabase.LoadedInstruments[index].ToUnmanaged(Allocator.Persistent);
            }
        }

        public static NativeArray<LoadedInstrument.Unmanaged> GetLoadedInstrumentsUnmanaged()
        {
            CheckUpdateLoadedInstrumentsUnmanaged();
            return LoadedInstrumentsUnmanaged.Data;
        }


        [ContextMenu("DeleteDatabase")]
        public void DeleteDatabase()
        {
            LoadedInstruments.Clear();
            for (int i = 0; i < LoadedInstrumentsUnmanaged.Data.Length; i++)
            {
                LoadedInstrumentsUnmanaged.Data[i].Dispose();
            }

            LoadedInstrumentsUnmanaged.Data.Dispose();
        }

        [BurstDiscard]
        private static void CheckUpdateLoadedInstrumentsUnmanaged()
        {
            if (LoadedInstrumentsUnmanaged.Data.IsCreated &&
                LoadedInstrumentsUnmanaged.Data.Length == AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count)
            {
                bool allGood = true;
                for (var i = 0; i < LoadedInstrumentsUnmanaged.Data.Length; i++)
                {
                    if (!LoadedInstrumentsUnmanaged.Data[i].Instrument
                            .Equals(AnywhenRuntime.InstrumentDatabase.LoadedInstruments[i].Instrument.ToUnmanaged()))
                    {
                        allGood = false;
                    }
                }

                if (allGood)
                    return;
            }

            if (LoadedInstrumentsUnmanaged.Data.IsCreated)
            {
                for (int i = 0; i < LoadedInstrumentsUnmanaged.Data.Length; i++)
                {
                    LoadedInstrumentsUnmanaged.Data[i].Dispose();
                }

                LoadedInstrumentsUnmanaged.Data.Dispose();
            }

            LoadedInstrumentsUnmanaged.Data =
                new NativeArray<LoadedInstrument.Unmanaged>(AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count, Allocator.Persistent);

            for (var index = 0; index < AnywhenRuntime.InstrumentDatabase.LoadedInstruments.Count; index++)
            {
                LoadedInstrumentsUnmanaged.Data[index] = AnywhenRuntime.InstrumentDatabase.LoadedInstruments[index].ToUnmanaged(Allocator.Persistent);
            }
        }

        private void OnDestroy()
        {
            if (LoadedInstrumentsUnmanaged.Data.IsCreated)
            {
                for (int i = 0; i < LoadedInstrumentsUnmanaged.Data.Length; i++)
                {
                    LoadedInstrumentsUnmanaged.Data[i].Dispose();
                }

                LoadedInstrumentsUnmanaged.Data.Dispose();
                LoadedInstrumentsUnmanaged.Data = default;
            }
        }


#if UNITY_EDITOR


        public static void LoadInstrumentNotes(AnywhenSampleInstrument instrument)
        {
            if (!instrument) return;
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
            if (!loadedInstruments.IsCreated) return default;

            for (var i = 0; i < loadedInstruments.Length; i++)
            {
                var loaded = loadedInstruments[i];
                var unmanaged = loaded.Instrument;
                unmanaged.seed = instrument.seed; // Ignore seed for comparison
                if (unmanaged.Equals(instrument))
                {
                    return loaded.Clips;
                }
            }


            LogErrorNoInstruments();
            return default;
        }

        [BurstDiscard]
        private static void LogErrorNoInstruments()
        {
            Debug.LogError("No instruments loaded in InstrumentDatabase!");
        }

        public static void LoadAllInstruments(AnysongObject currentSong)
        {
#if UNITY_EDITOR
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
                            Debug.LogWarning("Loaded instrument at runtime, make sure to load instruments from the editor - instrument: " +
                                             sampleInstrument.name);
                        }
                    }
                }
            }
#endif
        }
    }
}