using UnityEditor;

#if UNITY_EDITOR
using UnityEditor.AnimatedValues;

using UnityEngine;

namespace Anywhen.Composing.Editor
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
            _currentWidth = Mathf.Min(_textureMaxWidth, GetViewWidth());

            EditorGUI.DrawRect(new Rect(0, 0, Screen.width, 360), _anysongPlayer.songObject.playerBackgroundColor);

            GUI.DrawTexture(
                new Rect(GetViewWidth() / 2f - (_currentWidth / 2), 30, _currentWidth,
                    _currentWidth / _textureAspect), _texture);
            _anysongPlayer.songObject =
                (AnysongObject)EditorGUILayout.ObjectField("Song", _anysongPlayer.songObject, typeof(AnysongObject));


            GUILayoutUtility.GetRect(_currentWidth, _currentWidth, 200, 300);


            GUILayout.Space(-60);

            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.black;
            if (_anysongPlayer.songObject != null)
            {
                _anysongPlayer.variation =
                    EditorGUILayout.Slider("Macro 1", _anysongPlayer.variation, 0,
                        _anysongPlayer.songObject.Sections.Count - 1);
                _anysongPlayer.variation =
                    EditorGUILayout.Slider("Macro 2", _anysongPlayer.variation, 0,
                        _anysongPlayer.songObject.Sections.Count - 1);
            }

            _showExtraFields.target = EditorGUILayout.ToggleLeft("Show extra fields", _showExtraFields.target);

            //Extra block that can be toggled on and off.
            if (EditorGUILayout.BeginFadeGroup(_showExtraFields.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PrefixLabel("Color");
                EditorGUILayout.ColorField(Color.black);
                EditorGUILayout.PrefixLabel("Text");
                EditorGUILayout.TextField("test");
                EditorGUILayout.PrefixLabel("Number");
                EditorGUILayout.IntSlider(100, 0, 10);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();

            if (_anysongPlayer.songObject != _currentSongObject)
            {
                _texture = Resources.Load<Texture2D>("Songs/" + _anysongPlayer.songObject.coverGraphic);
                _currentSongObject = _anysongPlayer.songObject;
                _textureAspect = (float)_texture.width / _texture.height;

            }
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
