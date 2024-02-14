using Synth;
using Synth.Editor;
using UnityEditor;
using UnityEngine;

namespace UnitySynth.Runtime.Synth.Editor
{
    public class SynthLfoInspector : UnityEditor.Editor
    {
        public static void Draw(SynthSettingsInspector parent, SynthSettingsObjectLFO settings, string listName)
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                parent.DeleteElement<SynthSettingsObjectLFO>(settings, listName);
                parent.RebuildSynth();
            }

            EditorGUILayout.PrefixLabel("LFO", new GUIStyle { fontStyle = FontStyle.Bold });
            GUILayout.EndHorizontal();

            AnimationCurve curve = new AnimationCurve();
            int count = Mathf.Min((int)Mathf.Abs(settings.frequency * 20), 100);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float fadeIn = Mathf.InverseLerp(0, settings.fadeInDuration, t);
                curve.AddKey(t,
                    Mathf.Sin((t * 10) * settings.frequency)  *
                    fadeIn);
            }


            EditorGUILayout.CurveField(curve, Color.red, new Rect(0, -1, 1, 2), GUILayout.Height(60));

            //settings.amp = EditorGUILayout.Slider("Amp", settings.amp, .01f, 1);
            settings.frequency = EditorGUILayout.Slider("Frequency", settings.frequency, 0.1f, 100);
            settings.fadeInDuration = EditorGUILayout.Slider("Fade In Duration", settings.fadeInDuration, 0.001f, 15);

            settings.sendAmount = EditorGUILayout.Slider("Amount", settings.sendAmount, -100, 100);

            settings.retrigger = EditorGUILayout.Toggle("Retrigger", settings.retrigger);
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }
    }
}