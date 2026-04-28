using Anywhen.Synth;
using UnityEngine;
using UnityEditor;

namespace Anywhen.SettingsObjects
{
    [CustomPropertyDrawer(typeof(AudioProcessorSettings.LFOSettings))]
    public class LFOSettingsDrawer : PropertyDrawer
    {
        private const float PreviewHeight = 60f;
        private const float Spacing = 2f;
        private const float SliderHeight = 16f;
        private bool _foldout = true;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_foldout)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight + // Foldout line
                   PreviewHeight + Spacing +           // LFO wave preview
                   (SliderHeight + Spacing) * 2;       // 3 controls with spacing
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var frequencyProp = property.FindPropertyRelative("frequency");
            

            
            // Create foldout header that controls the enabled state
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            _foldout = EditorGUI.Foldout(foldoutRect, _foldout, label, true);

            if (EditorGUI.EndChangeCheck())
            {

                // Initialize default values when enabling for the first time
                if (_foldout && frequencyProp.floatValue == 0 )
                {
                    
                    frequencyProp.floatValue = 1f;
                }
            }

            if (_foldout)
            {
                // Draw LFO wave preview
                var previewRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight,
                    position.width,
                    PreviewHeight
                );
                DrawLFOPreview(previewRect, frequencyProp.floatValue, _foldout);

                float yPos = position.y + EditorGUIUtility.singleLineHeight + PreviewHeight + Spacing;
                float indent = 15f;

                // Draw controls for LFO parameters (no need for enabled toggle anymore)
                var controlRect = new Rect(position.x + indent, yPos, position.width - indent, SliderHeight);
                EditorGUI.PropertyField(controlRect, frequencyProp);

               
            }

            EditorGUI.EndProperty();
        }

        private void DrawLFOPreview(Rect rect, float frequency, bool enabled)
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
            Handles.color = Color.white;

            int steps = Mathf.RoundToInt(rect.width / 2f); // Number of line segments
            float timeSpan = 4f; // Show 4 seconds worth of LFO

            Vector3[] points = new Vector3[steps];

            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / (steps - 1);
                float x = rect.x + 2 + t * (rect.width - 4);
                float time = t * timeSpan;
                float lfoValue = Mathf.Sin(time * frequency * 2f * Mathf.PI) ;
                float y = centerY - lfoValue * (rect.height * 0.4f); // Scale to fit in preview

                points[i] = new Vector3(x, y);
            }

            // Draw the waveform
            for (int i = 0; i < points.Length - 1; i++)
            {
                Handles.DrawLine(points[i], points[i + 1]);
            }
            
        }
    }
}