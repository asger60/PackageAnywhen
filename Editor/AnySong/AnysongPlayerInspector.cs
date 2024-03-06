#if UNITY_EDITOR
using Anywhen.Composing;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Editor.Anysong
{
    [CustomEditor(typeof(AnysongPlayer))]
    public class AnysongPlayerInspector : UnityEditor.Editor
    {
        private AnysongPlayer _anysongPlayer;
        Texture2D _texture;
        private float _textureAspect;
        private float _textureMaxWidth = 800;
        private float _currentWidth;

        AnimBool _showExtraFields;
        private AnysongObject _currentSongObject;

        private void OnEnable()
        {
            _anysongPlayer = (AnysongPlayer)target;
            _textureMaxWidth = 800;

            _showExtraFields = new AnimBool(false);
            _showExtraFields.valueChanged.AddListener(Repaint);
        }


        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
   
        }

        private Rect _rect;

        private float GetViewWidth()
        {
            GUILayout.Label("hack", GUILayout.MaxHeight(0));
            if (Event.current.type == EventType.Repaint)
            {
                // hack to get real view width
                _rect = GUILayoutUtility.GetLastRect();
            }

            return _rect.width + 22;
        }
    }
}
#endif
