using System;
using System.Collections.Generic;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[Serializable]
public class AnySection
{
    public int rootNote;
    public AnywhenScaleObject scale;

    [Range(0, 1f)] public float volume = 0.85f;

    public List<AnyTrack> tracks;

    public void Init()
    {
        volume = 1f;
        rootNote = 0;
        tracks = new List<AnyTrack> { new AnyTrack() };
        foreach (var track in tracks)
        {
            track.Init();
        }
    }


    [Serializable]
    public class AnyTrack
    {
        [Range(0, 1f)] public float volume;
        public AnywhenInstrument instrument;
        public List<AnyPattern> patterns;

        public void Init()
        {
            volume = 1;
            patterns = new List<AnyPattern> { new AnyPattern() };
            foreach (var pattern in patterns)
            {
                pattern.Init();
            }
        }

        public AnyTrack Clone()
        {
            var clone = new AnyTrack
            {
                patterns = new List<AnyPattern>()
            };
            for (var i = 0; i < 16; i++)
            {
                clone.patterns.Add(patterns[i].Clone());
            }

            clone.volume = volume;
            
            return clone;
        }
    }

    [Serializable]
    public class AnyPattern
    {
        public int[] triggerBars;
        [Range(0, 1f)] public float triggerChance;
        public List<AnyPatternStep> steps;

        public void Init()
        {
            triggerBars = new[] { 0 };
            triggerChance = 1;
            steps = new List<AnyPatternStep>();
            for (int i = 0; i < 16; i++)
            {
                steps.Add(new AnyPatternStep());
            }
        }

        public AnyPattern Clone()
        {
            var clone = new AnyPattern
            {
                steps = new List<AnyPatternStep>()
            };
            for (var i = 0; i < 16; i++)
            {
                clone.steps.Add(steps[i].Clone());
            }

            clone.triggerChance = triggerChance;
            return clone;
        }
    }

    [Serializable]
    public class AnyPatternStep
    {
        public bool noteOn;
        public bool noteOff;
        public float duration;
        public float offset;
        public float velocity;

        [Range(0, 1f)] public float mixWeight;

        public List<int> notes;
        public int noteRandom;


        [Range(0, 1f)] public float chance = 1;
        [Range(0, 1f)] public float expression = 0;

        public int GetNote()
        {
            return notes.Count > 1 ? notes[Random.Range(0, notes.Count)] : notes[0];
        }

        public int[] GetNotes()
        {
            int[] r = new int[notes.Count];
            for (var i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                r[i] = note + Random.Range(-noteRandom, noteRandom);
            }

            return r;
        }

        public AnyPatternStep()
        {
            duration = 1;
            expression = 1;
            velocity = 1;
            notes = new List<int> { 0 };
            mixWeight = 0.5f;
        }

        public AnyPatternStep Clone()
        {
            var clone = (AnyPatternStep)MemberwiseClone();
            clone.notes = new List<int>();
            foreach (var note in notes)
            {
                clone.notes.Add(note);
            }


            return clone;
        }
    }
}