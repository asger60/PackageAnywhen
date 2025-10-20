
using UnityEngine;
using UnityEditor;

namespace Anywhen.SettingsObjects
{
    [CustomPropertyDrawer(typeof(AnywhenSampleInstrument.PitchLFOSettings))]
    public class PitchLFOSettingsDrawer : PropertyDrawer
    {
        private const float PreviewHeight = 60f;
        private const float Spacing = 2f;
        private const float SliderHeight = 16f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var enabledProp = property.FindPropertyRelative("enabled");
            bool isEnabled = enabledProp.boolValue;
            
            if (!isEnabled)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight + // Foldout line
                   PreviewHeight + Spacing + // LFO wave preview
                   (SliderHeight + Spacing) * 3; // 3 controls with spacing (frequency, amplitude, retrigger - enabled is now the foldout)
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var enabledProp = property.FindPropertyRelative("enabled");
            var frequencyProp = property.FindPropertyRelative("frequency");
            var amplitudeProp = property.FindPropertyRelative("amplitude");
            var retriggerProp = property.FindPropertyRelative("retrigger");

            // Create foldout header that controls the enabled state
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.BeginChangeCheck();
            bool newFoldoutState = EditorGUI.Foldout(foldoutRect, enabledProp.boolValue, label, true);
            
            if (EditorGUI.EndChangeCheck())
            {
                enabledProp.boolValue = newFoldoutState;
                
                // Initialize default values when enabling for the first time
                if (newFoldoutState && frequencyProp.floatValue == 0 && amplitudeProp.floatValue == 0)
                {
                    frequencyProp.floatValue = 1f;
                    amplitudeProp.floatValue = 0.1f;
                }
            }

            if (enabledProp.boolValue)
            {
                // Draw LFO wave preview
                var previewRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight,
                    position.width,
                    PreviewHeight
                );
                DrawLFOPreview(previewRect, frequencyProp.floatValue, amplitudeProp.floatValue, enabledProp.boolValue);

                float yPos = position.y + EditorGUIUtility.singleLineHeight + PreviewHeight + Spacing;
                float indent = 15f;

                // Draw controls for LFO parameters (no need for enabled toggle anymore)
                var controlRect = new Rect(position.x + indent, yPos, position.width - indent, SliderHeight);
                EditorGUI.PropertyField(controlRect, frequencyProp);

                yPos += SliderHeight + Spacing;
                controlRect.y = yPos;
                EditorGUI.PropertyField(controlRect, amplitudeProp);

                yPos += SliderHeight + Spacing;
                controlRect.y = yPos;
                EditorGUI.PropertyField(controlRect, retriggerProp);
            }

            EditorGUI.EndProperty();
        }

        private void DrawLFOPreview(Rect rect, float frequency, float amplitude, bool enabled)
        {
            // Draw the background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            if (!enabled)
            {
                // Draw disabled state
                var disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                EditorGUI.DrawRect(rect, disabledColor);
                
                var centeredTextRect = new Rect(rect.x, rect.y + rect.height * 0.5f - 8f, rect.width, 16f);
                var oldColor = GUI.color;
                GUI.color = Color.gray;
                EditorGUI.LabelField(centeredTextRect, "LFO Disabled", EditorStyles.centeredGreyMiniLabel);
                GUI.color = oldColor;
                return;
            }

            // Draw center line
            Handles.color = new Color(0.4f, 0.4f, 0.4f);
            float centerY = rect.y + rect.height * 0.5f;
            Handles.DrawLine(new Vector3(rect.x + 2, centerY), new Vector3(rect.x + rect.width - 2, centerY));

            // Draw LFO waveform (sine wave)
            Handles.color = Color.cyan;
            
            int steps = Mathf.RoundToInt(rect.width / 2f); // Number of line segments
            float timeSpan = 4f; // Show 4 seconds worth of LFO
            
            Vector3[] points = new Vector3[steps];
            
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / (steps - 1);
                float x = rect.x + 2 + t * (rect.width - 4);
                float time = t * timeSpan;
                float lfoValue = Mathf.Sin(time * frequency * 2f * Mathf.PI) * amplitude;
                float y = centerY - lfoValue * (rect.height * 0.4f); // Scale to fit in preview
                
                points[i] = new Vector3(x, y);
            }
            
            // Draw the waveform
            for (int i = 0; i < points.Length - 1; i++)
            {
                Handles.DrawLine(points[i], points[i + 1]);
            }

            // Draw amplitude bounds
            Handles.color = new Color(1f, 1f, 0f, 0.3f);
            float maxY = centerY - amplitude * (rect.height * 0.4f);
            float minY = centerY + amplitude * (rect.height * 0.4f);
            
            Handles.DrawDottedLine(new Vector3(rect.x + 2, maxY), new Vector3(rect.x + rect.width - 2, maxY), 3f);
            Handles.DrawDottedLine(new Vector3(rect.x + 2, minY), new Vector3(rect.x + rect.width - 2, minY), 3f);
        }
    }
}