using System.Collections.Generic;
using System.Linq;
using Anywhen.Synth;
using UnityEditor;
using UnityEngine;
using UnitySynth.Runtime.Synth;

namespace Anywhen.SettingsObjects
{
    [CreateAssetMenu(fileName = "New synth preset", menuName = "UnitySynth/SynthPreset")]
    public class AnywhenSynthPreset : AnywhenInstrument
    {
        public bool isInit;

        public bool unison;
        public int voices = 3;
        public float voiceSpread;


        public SynthSettingsObjectOscillator[] oscillatorSettings;
        public SynthSettingsObjectFilter[] filterSettings;
        public SynthSettingsObjectBase[] pitchModifiers;
        public SynthSettingsObjectBase[] amplitudeModifiers;
        public SynthSettingsObjectBase[] filterModifiers;


        private AnywhenSynth _runtimeSynth;

        public void BindToRuntime(AnywhenSynth synth)
        {
            _runtimeSynth = synth;
        }

        public void RebuildSynth()
        {
            if (_runtimeSynth != null)
                _runtimeSynth.RebuildSynth();
        }
#if UNITY_EDITOR
        [ContextMenu("Clean up preset object")]
        public void CleanUpPreset()
        {
            var controls = GetSubObjectsOfType<SynthSettingsObjectBase>(this);
            for (var i = controls.Count - 1; i >= 0; i--)
            {
                var control = controls[i];
                if (filterModifiers.Contains(control) ||
                    amplitudeModifiers.Contains(control) || pitchModifiers.Contains(control))
                    continue;

                if (filterSettings.Contains(control) || oscillatorSettings.Contains(control))
                    continue;

                DestroyImmediate(control, true);
            }

            oscillatorSettings = oscillatorSettings.Where(x => x != null).ToArray();
            filterSettings = filterSettings.Where(x => x != null).ToArray();
            pitchModifiers = pitchModifiers.Where(x => x != null).ToArray();
            amplitudeModifiers = amplitudeModifiers.Where(x => x != null).ToArray();
            filterModifiers = filterModifiers.Where(x => x != null).ToArray();
        }

        private static List<T> GetSubObjectsOfType<T>(Object asset) where T : Object
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));
            List<T> ofType = new List<T>();
            foreach (Object o in objs)
            {
                if (o is T)
                {
                    ofType.Add(o as T);
                }
            }

            return ofType;
        }
#endif

        private void Reset()
        {
            isInit = false;
        }
    }
}