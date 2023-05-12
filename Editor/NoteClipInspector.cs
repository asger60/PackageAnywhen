using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(AnywhenNoteClip))]
    [CanEditMultipleObjects]
    public class NoteClipInspector : UnityEditor.Editor
    {
        SerializedProperty loopStart;
        SerializedProperty loopLength;
        SerializedProperty frequency;
        SerializedProperty channels;
        private AudioClip _editorClip;
        private AnywhenNoteClip _target;
        private int _length = 100;

        void OnEnable()
        {
            _target = (AnywhenNoteClip)target;
            loopStart = serializedObject.FindProperty("loopStart");
            loopLength = serializedObject.FindProperty("loopLength");
            frequency = serializedObject.FindProperty("frequency");
            channels = serializedObject.FindProperty("channels");
            
            //_length = _target.clipSamples.Length;
            //_editorClip = AudioClip.Create("editorClip", _length, _target.channels, _target.frequency, false);
//
            //float[] noise = new float[_length];
//
            //for (var i = 0; i < noise.Length; i++)
            //{
            //    noise[i] = Random.Range(-1f, 1);
            //}
//
            //_editorClip.SetData(_target.clipSamples, 0);
            
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(loopStart);
            EditorGUILayout.PropertyField(loopLength);
            EditorGUILayout.PropertyField(frequency);
            EditorGUILayout.PropertyField(channels);
            EditorGUILayout.LabelField("Samples", _target.clipSamples.Length.ToString());

            //if (editorClip.samples > 0)
            //{
            //     Debug.Log(AssetPreview.GetAssetPreview(editorClip as AudioClip));
            //     GUILayout.Box(AssetPreview.GetAssetPreview(editorClip), GUILayout.Height(70), GUILayout.Width(70));
//
            //}

            if (GUILayout.Button("PLAY"))
            {
                var player = FindObjectOfType<NoteClipPlayer>();
                player.PlayScheduled(0, _target);
                
                
                AudioClip newAudioClip =
                    AudioClip.Create("source", _target.clipSamples.Length, _target.channels, _target.frequency,
                        false);

                newAudioClip.SetData(_target.clipSamples, 0);
                _target.sourceClip = newAudioClip;
                newAudioClip.LoadAudioData();
                Debug.Log(newAudioClip.samples);
                //AssetDatabase.CreateAsset(newAudioClip, "Assets/testasset.asset");
               // PlayClip(newAudioClip);


            }



            serializedObject.ApplyModifiedProperties();
        }

        
    }
}