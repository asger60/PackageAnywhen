using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Anywhen.Composing;
using Anywhen.SettingsObjects;
using UnityEngine;
using UnityEngine.Audio;

namespace Anywhen
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class AnywhenRuntime : MonoBehaviour
    {
        private static AnywhenConductor _conductor;
        public static AnywhenConductor Conductor => _conductor;

        private static AnywhenSampleNoteClipPreviewer _sampleNoteClipPreviewer;


        [SerializeField] InstrumentDatabase _thisInstrumentDatabase;
        private static InstrumentDatabase _instrumentDatabase;
         AnywhenAudioMetronome _metronome;
        public static AnywhenAudioMetronome Metronome => _instance._metronome;

        public static InstrumentDatabase InstrumentDatabase => Instance._thisInstrumentDatabase;

        private static AnywhenRuntime _instance;

        private static AnywhenRuntime Instance => _instance;


        private bool _isPreviewing;
        [SerializeField] private bool logErrors;
        public static bool IsPreviewing => Instance._isPreviewing;
        private AnywhenSampleNoteClipPreviewer _noteClipPreviewer;

#if UNITY_EDITOR
        static AnywhenRuntime()
        {
            EditorApplication.update += EditorUpdate;
        }

        static void EditorUpdate()
        {
            if (Application.isPlaying) return;
            FindReferences();
        }

        static void FindReferences()
        {
            if (_instance == null)
            {
                _instance = FindObjectsByType<AnywhenRuntime>()[0];
                if (_instance != null)
                {
                    _instance.GetAnyComponents();
                }
            }
        }
#endif


        private void Awake()
        {
            _instance = this;
            GetAnyComponents();
        }


        public void Init()
        {
            GetAnyComponents();
        }


        void GetAnyComponents()
        {
            TryGetComponent(out _conductor);
            _instrumentDatabase = GetComponentInChildren<InstrumentDatabase>();
            AudioSource a = GetComponent<AudioSource>();
            a.Play();
            _metronome = a.generator as AnywhenAudioMetronome;
            Debug.Log(_metronome);
        }

        public enum DebugMessageType
        {
            Log,
            Warning,
            Error
        }

        public static void Log(string message, DebugMessageType debugMessageType = DebugMessageType.Log)
        {
            if (!Instance.logErrors) return;
            switch (debugMessageType)
            {
                case DebugMessageType.Log:
                    Debug.Log(message);
                    break;
                case DebugMessageType.Warning:
                    Debug.LogWarning(message);

                    break;
                case DebugMessageType.Error:
                    Debug.LogError(message);
                    break;
            }
        }

        public static void PreviewNoteClip(AnywhenNoteClip anywhenNoteClip)
        {
            if (!Instance._noteClipPreviewer)
                Instance._noteClipPreviewer = Instance.FindOrCreatePreviewer();

            Instance._noteClipPreviewer.PlayNoteClip(anywhenNoteClip);
        }

        public static void PreviewNoteClip(AnywhenSampleInstrument.AnywhenNoteClipPlaybackSettings anywhenNoteClip)
        {
            if (!Instance._noteClipPreviewer)
                Instance._noteClipPreviewer = Instance.FindOrCreatePreviewer();

            Instance._noteClipPreviewer.PlayNoteClip(anywhenNoteClip.noteClip);
        }

        public static void StopNoteClipPreview(AnywhenNoteClip anywhenNoteClip)
        {
            if (!Instance._noteClipPreviewer) return;
            Instance._noteClipPreviewer.StopClip();
        }

        private AnywhenSampleNoteClipPreviewer FindOrCreatePreviewer()
        {
            // Look for an existing previewer
            var previewers =
                FindObjectsByType<AnywhenSampleNoteClipPreviewer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AnywhenSampleNoteClipPreviewer previewer = null;
            if (previewers.Length > 0)
                previewer = previewers[0];

            // If none exists, create a new one
            if (!previewer)
            {
                var previewerObject = new GameObject("NoteClipPreviewer");
                previewer = previewerObject.AddComponent<AnywhenSampleNoteClipPreviewer>();
                previewerObject.hideFlags = HideFlags.HideAndDontSave;
            }

            return previewer;
        }

        public static void SetTempo(int newTempo)
        {
            _instance._metronome.SetTempo(newTempo);
        }
    }
}