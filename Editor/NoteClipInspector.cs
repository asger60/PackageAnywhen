using Anywhen.SettingsObjects;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

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

        void OnEnable()
        {
            _target = (AnywhenNoteClip)target;
            loopStart = serializedObject.FindProperty("loopStart");
            loopLength = serializedObject.FindProperty("loopLength");
            frequency = serializedObject.FindProperty("frequency");
            channels = serializedObject.FindProperty("channels");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(loopStart);
            EditorGUILayout.PropertyField(loopLength);
            EditorGUILayout.PropertyField(frequency);
            EditorGUILayout.PropertyField(channels);
            EditorGUILayout.LabelField("Samples", _target.clipSamples.Length.ToString());


            if (GUILayout.Button("PLAY"))
            {
                AudioClip newAudioClip =
                    AudioClip.Create("source", _target.clipSamples.Length, _target.channels, _target.frequency,
                        false);

                newAudioClip.SetData(_target.clipSamples, 0);
                _target.sourceClip = newAudioClip;
                newAudioClip.LoadAudioData();
                Debug.Log(newAudioClip.samples);
                //AssetDatabase.CreateAsset(newAudioClip, "Assets/testasset.asset");
                PlayClip(newAudioClip);
            }


            serializedObject.ApplyModifiedProperties();
        }

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            Debug.Log(method);
            method.Invoke(
                null,
                new object[] { clip, startSample, loop }
            );
        }

        public static void StopAllClips()
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { },
                null
            );

            Debug.Log(method);
            method.Invoke(
                null,
                new object[] { }
            );
        }
    }
}