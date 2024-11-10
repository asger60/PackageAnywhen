using System;
using UnityEditor;
using UnityEngine;


public class SynthOSCInspector : UnityEditor.Editor
{
    public static void Draw(SynthSettingsInspector parent, SynthSettingsObjectOscillator settings)
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            parent.DeleteElement<SynthSettingsObjectOscillator>(settings, "oscillatorSettings");
            parent.RebuildSynth();
        }

        EditorGUILayout.PrefixLabel("Voice", new GUIStyle { fontStyle = FontStyle.Bold });
        GUILayout.EndHorizontal();


        var types = Enum.GetNames(typeof(SynthSettingsObjectOscillator.OscillatorType));


        EditorGUI.BeginChangeCheck();

        GUILayout.Space(10);

        settings.oscillatorType =
            (SynthSettingsObjectOscillator.OscillatorType)GUILayout.SelectionGrid((int)settings.oscillatorType,
                types, 3);

        GUILayout.Space(5);

        if (EditorGUI.EndChangeCheck())
        {
            if (Application.isPlaying)
            {
                parent.RebuildSynth();
            }
        }

        settings.amplitude = EditorGUILayout.Slider("Volume", settings.amplitude, 0f, 1f);


        switch (settings.oscillatorType)
        {
            case SynthSettingsObjectOscillator.OscillatorType.Simple:
                settings.tuning = (int)EditorGUILayout.Slider("Tuning", settings.tuning, -48, 48);
                settings.simpleOscillatorType =
                    (SynthSettingsObjectOscillator.SimpleOscillatorTypes)EditorGUILayout.EnumPopup(
                        "Waveform", settings.simpleOscillatorType);
                break;
            case SynthSettingsObjectOscillator.OscillatorType.WaveTable:
                settings.tuning = (int)EditorGUILayout.Slider("Tuning", settings.tuning, -48, 48);
                EditorGUILayout.Slider("Resolution", 32, 8, 128);
                settings.waveTableOscillatorType =
                    (SynthSettingsObjectOscillator.WaveTableOscillatorTypes)EditorGUILayout.EnumPopup(
                        "Wavetable", settings.waveTableOscillatorType);
                break;
            case SynthSettingsObjectOscillator.OscillatorType.Noise:
                settings.noiseType =
                    (SynthSettingsObjectOscillator.NoiseTypes)EditorGUILayout.EnumPopup(
                        "Noise type", settings.noiseType);
                break;
        }


        GUILayout.EndVertical();

        GUILayout.Space(10);
    }
}