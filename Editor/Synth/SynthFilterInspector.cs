using System;
using UnityEditor;
using UnityEngine;
using UnitySynth.Runtime.Synth;


public class SynthFilterInspector : UnityEditor.Editor
{
    public static void Draw(SynthSettingsInspector parent, SynthSettingsObjectFilter settings)
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            parent.DeleteElement<SynthSettingsObjectFilter>(settings, "filterSettings");
            parent.RebuildSynth();
        }

        GUILayout.EndHorizontal();

        //AnimationCurve curve = new AnimationCurve();
        //EditorGUILayout.CurveField(curve, Color.red, new Rect(0, -1, 1, 2), GUILayout.Height(60));

        var newFilterType =
            (SynthSettingsObjectFilter.FilterTypes)EditorGUILayout.EnumPopup("Filter type:", settings.filterType);

        if (newFilterType != settings.filterType)
        {
            settings.filterType = newFilterType;
            parent.RebuildSynth();
        }

        switch (settings.filterType)
        {
            case SynthSettingsObjectFilter.FilterTypes.LowPass:

                settings.lowPassSettings.oversampling = EditorGUILayout.IntSlider("Oversampling",
                    settings.lowPassSettings.oversampling, 1, 4);
                settings.lowPassSettings.cutoffFrequency =
                    EditorGUILayout.Slider("CutOff", settings.lowPassSettings.cutoffFrequency, 1, 24000);
                settings.lowPassSettings.resonance =
                    EditorGUILayout.Slider("Resonance", settings.lowPassSettings.resonance, 0, 1);

                break;
            case SynthSettingsObjectFilter.FilterTypes.BandPass:
                settings.bandPassSettings.frequency = EditorGUILayout.Slider("Frequency",
                    settings.bandPassSettings.frequency, 1, 24000);
                settings.bandPassSettings.bandWidth = EditorGUILayout.Slider("Bandwidth",
                    settings.bandPassSettings.bandWidth, 1, 100);
                break;
            case SynthSettingsObjectFilter.FilterTypes.Formant:
                settings.formantSettings.vowel = EditorGUILayout.IntSlider("Vowel",
                    settings.formantSettings.vowel, 1, 6);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        GUILayout.EndVertical();
        GUILayout.Space(10);
    }
}