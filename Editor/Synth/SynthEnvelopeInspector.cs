using UnityEditor;
using UnityEngine;
using UnitySynth.Runtime.Synth;
using UnitySynth.Runtime.Synth.Editor;

namespace Synth.Editor
{
    public class SynthEnvelopeInspector : UnityEditor.Editor
    {
        public static void Draw(SynthSettingsInspector parent, SynthSettingsObjectEnvelope settings, string listName)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                parent.DeleteElement<SynthSettingsObjectEnvelope>(settings, listName);
                parent.RebuildSynth();
            }

            EditorGUILayout.PrefixLabel("Envelope", new GUIStyle { fontStyle = FontStyle.Bold });


            EditorGUILayout.EndHorizontal();

            AnimationCurve curve = new AnimationCurve();


            float curveLength = settings.attack + settings.decay + settings.release + 1;

            curve.AddKey(0, 0);

            float attackLength = settings.attack / (curveLength);
            float decayLength = settings.decay / (curveLength + 0.1f);
            float releaseLength = settings.release / (curveLength + 0.1f);

            curve.AddKey(curveLength * attackLength, 1);
            curve.AddKey(curveLength * (attackLength + decayLength), settings.sustain);
            curve.AddKey(curveLength - (curveLength * releaseLength), settings.sustain);


            curve.AddKey(curveLength, 0);
            SetCurveLinear(curve);


            EditorGUILayout.CurveField(curve, Color.yellow, new Rect(0, 0, curveLength, 1), GUILayout.Height(60));

            settings.attack = EditorGUILayout.Slider("Attack", settings.attack, 0.01f, 5);
            settings.decay = EditorGUILayout.Slider("Decay", settings.decay, 0.01f, 5);
            settings.sustain = EditorGUILayout.Slider("Sustain", settings.sustain, 0.001f, 1);
            settings.release = EditorGUILayout.Slider("Release", settings.release, 0.01f, 5);

            settings.sendAmount = EditorGUILayout.Slider("Send Amount", settings.sendAmount, -100f, 100f);

            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private static void SetCurveLinear(AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; ++i)
            {
                float intangent = 0;
                float outtangent = 0;
                bool intangent_set = false;
                bool outtangent_set = false;
                Vector2 point1;
                Vector2 point2;
                Vector2 deltapoint;
                Keyframe key = curve[i];

                if (i == 0)
                {
                    intangent = 0;
                    intangent_set = true;
                }

                if (i == curve.keys.Length - 1)
                {
                    outtangent = 0;
                    outtangent_set = true;
                }

                if (!intangent_set)
                {
                    point1.x = curve.keys[i - 1].time;
                    point1.y = curve.keys[i - 1].value;
                    point2.x = curve.keys[i].time;
                    point2.y = curve.keys[i].value;

                    deltapoint = point2 - point1;

                    intangent = deltapoint.y / deltapoint.x;
                }

                if (!outtangent_set)
                {
                    point1.x = curve.keys[i].time;
                    point1.y = curve.keys[i].value;
                    point2.x = curve.keys[i + 1].time;
                    point2.y = curve.keys[i + 1].value;

                    deltapoint = point2 - point1;

                    outtangent = deltapoint.y / deltapoint.x;
                }

                key.inTangent = intangent;
                key.outTangent = outtangent;
                curve.MoveKey(i, key);
            }
        }
    }
}