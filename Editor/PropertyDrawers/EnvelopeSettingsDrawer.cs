using UnityEngine;
using UnityEditor;

namespace Anywhen.SettingsObjects
{
    [CustomPropertyDrawer(typeof(AnywhenSampleInstrument.EnvelopeSettings))]
    public class EnvelopeSettingsDrawer : PropertyDrawer
    {
        private const float PreviewHeight = 80f;
        private const float Spacing = 2f;
        private const float SliderHeight = 16f;
        private bool _foldout = true;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_foldout)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight + // Foldout line
                   PreviewHeight + Spacing + // ADSR envelope preview
                   (SliderHeight + Spacing) * 4; // 4 sliders with spacing
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var attackProp = property.FindPropertyRelative("attack");
            var decayProp = property.FindPropertyRelative("decay");
            var sustainProp = property.FindPropertyRelative("sustain");
            var releaseProp = property.FindPropertyRelative("release");

            // Create foldout header
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            _foldout = EditorGUI.Foldout(foldoutRect, _foldout, label, true);

            if (_foldout)
            {
                // Draw envelope preview
                var previewRect = new Rect(
                    position.x,
                    position.y + EditorGUIUtility.singleLineHeight,
                    position.width,
                    PreviewHeight
                );
                DrawEnvelopePreview(previewRect, attackProp.floatValue, decayProp.floatValue,
                    sustainProp.floatValue, releaseProp.floatValue);

                float yPos = position.y + EditorGUIUtility.singleLineHeight + PreviewHeight + Spacing;
                float indent = 15f;

                // Draw sliders for ADSR values
                var sliderRect = new Rect(position.x + indent, yPos, position.width - indent, SliderHeight);
                EditorGUI.PropertyField(sliderRect, attackProp);

                yPos += SliderHeight + Spacing;
                sliderRect.y = yPos;
                EditorGUI.PropertyField(sliderRect, decayProp);

                yPos += SliderHeight + Spacing;
                sliderRect.y = yPos;
                EditorGUI.PropertyField(sliderRect, sustainProp);

                yPos += SliderHeight + Spacing;
                sliderRect.y = yPos;
                EditorGUI.PropertyField(sliderRect, releaseProp);
            }

            EditorGUI.EndProperty();
        }

        private void DrawEnvelopePreview(Rect rect, float attack, float decay, float sustain, float release)
        {
            // Draw the background
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            // Calculate envelope points
            float totalTime = attack + decay + 1.0f + release; // 1.0 for sustain period
            float attackWidth = (attack / totalTime) * rect.width;
            float decayWidth = (decay / totalTime) * rect.width;
            float sustainWidth = (1.0f / totalTime) * rect.width;
            float releaseWidth = (release / totalTime) * rect.width;

            float height = rect.height - 4;
            float sustainHeight = sustain * height;

            // Create line texture
            Handles.color = Color.white;

            // Attack line (0 to 1)
            Vector3 start = new Vector3(rect.x + 2, rect.y + rect.height - 2);
            Vector3 attackPeak = new Vector3(rect.x + 2 + attackWidth, rect.y + 2);
            Handles.DrawLine(start, attackPeak);

            // Decay line (1 to sustain)
            Vector3 sustainPoint = new Vector3(rect.x + 2 + attackWidth + decayWidth, rect.y + height - sustainHeight + 2);
            Handles.DrawLine(attackPeak, sustainPoint);

            // Sustain line (horizontal at sustain level)
            Vector3 sustainEnd = new Vector3(rect.x + 2 + attackWidth + decayWidth + sustainWidth,
                rect.y + height - sustainHeight + 2);
            Handles.DrawLine(sustainPoint, sustainEnd);

            // Release line (sustain to 0)
            Vector3 end = new Vector3(rect.x + 2 + attackWidth + decayWidth + sustainWidth + releaseWidth,
                rect.y + rect.height - 2);
            Handles.DrawLine(sustainEnd, end);
        }
    }
}