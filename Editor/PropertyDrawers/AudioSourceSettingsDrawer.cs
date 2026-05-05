using Anywhen.Synth;
using UnityEngine;
using UnityEditor;

namespace Anywhen.SettingsObjects
{
    [CustomPropertyDrawer(typeof(AudioSourceSettings))]
    public class AudioSourceSettingsDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("audioSourceType");
            var type = (AudioSourceSettings.AudioSourceTypes)typeProp.enumValueIndex;

            float height = EditorGUIUtility.singleLineHeight + Spacing; // audioSourceType

            if (type == AudioSourceSettings.AudioSourceTypes.Sample)
            {
                height += (EditorGUIUtility.singleLineHeight + Spacing) * 2; // sampleInstrument + sourceVolume
            }
            else if (type == AudioSourceSettings.AudioSourceTypes.Synth)
            {
                height += (EditorGUIUtility.singleLineHeight + Spacing) * 4; // synthType + sourceVolume + noteOffset + detune
            }
            else if (type == AudioSourceSettings.AudioSourceTypes.Noise)
            {
                height += (EditorGUIUtility.singleLineHeight + Spacing) * 2; // noiseType + sourceVolume
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeProp = property.FindPropertyRelative("audioSourceType");
            var type = (AudioSourceSettings.AudioSourceTypes)typeProp.enumValueIndex;

            float yPos = position.y;
            var lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(lineRect, typeProp, new GUIContent("Source Type"));
            yPos += EditorGUIUtility.singleLineHeight + Spacing;

            EditorGUI.indentLevel++;

            if (type == AudioSourceSettings.AudioSourceTypes.Sample)
            {
                var sampleSettings = property.FindPropertyRelative("sampleSourceSettings");

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, sampleSettings.FindPropertyRelative("sampleInstrument"), new GUIContent("Instrument"));
                yPos += EditorGUIUtility.singleLineHeight + Spacing;

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, sampleSettings.FindPropertyRelative("sourceVolume"), new GUIContent("Volume"));
            }
            else if (type == AudioSourceSettings.AudioSourceTypes.Synth)
            {
                var synthSettings = property.FindPropertyRelative("synthSourceSettings");

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, synthSettings.FindPropertyRelative("synthType"), new GUIContent("Synth Type"));
                yPos += EditorGUIUtility.singleLineHeight + Spacing;

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, synthSettings.FindPropertyRelative("sourceVolume"), new GUIContent("Volume"));
                yPos += EditorGUIUtility.singleLineHeight + Spacing;

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, synthSettings.FindPropertyRelative("noteOffset"), new GUIContent("Note Offset"));
                yPos += EditorGUIUtility.singleLineHeight + Spacing;

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, synthSettings.FindPropertyRelative("detune"), new GUIContent("Detune"));
            }
            else if (type == AudioSourceSettings.AudioSourceTypes.Noise)
            {
                var noiseSettings = property.FindPropertyRelative("noiseSourceSettings");

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, noiseSettings.FindPropertyRelative("noiseType"), new GUIContent("Noise Type"));
                yPos += EditorGUIUtility.singleLineHeight + Spacing;

                lineRect = new Rect(position.x, yPos, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(lineRect, noiseSettings.FindPropertyRelative("sourceVolume"), new GUIContent("Volume"));
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
