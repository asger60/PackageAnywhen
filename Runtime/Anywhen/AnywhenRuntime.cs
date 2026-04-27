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
        public static List<GeneratorInstance> ActiveGenerators = new List<GeneratorInstance>();

        private static AnywhenMetronome _metronome;

        public static AnywhenMetronome Metronome
        {
            get
            {
                if (!_metronome)
                    Instance?.GetAnyComponents();

                return _metronome;
            }
        }

        private static AnywhenConductor _conductor;
        public static AnywhenConductor Conductor => _conductor;

        private static AnywhenSampleNoteClipPreviewer _sampleNoteClipPreviewer;

        public static AnysongPlayerBrain AnysongPlayerBrain
        {
            get
            {
                if (!_anysongPlayerBrain)
                    Instance.GetAnyComponents();

                return _anysongPlayerBrain;
            }
        }

        private static AnysongPlayerBrain _anysongPlayerBrain;


        [SerializeField] InstrumentDatabase _thisInstrumentDatabase;
        private static InstrumentDatabase _instrumentDatabase;

        public static InstrumentDatabase InstrumentDatabase
        {
            get { return Instance._thisInstrumentDatabase; }
        }

        private static AnywhenRuntime _instance;

        private static AnywhenRuntime Instance => _instance;

        private static AnywhenPlayer _targetPlayer;


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


        public static AnywhenSampleNoteClipPreviewer ClipSampleNoteClipPreviewer
        {
            get
            {
                if (_sampleNoteClipPreviewer == null)
                    Instance.GetAnyComponents();
                return _sampleNoteClipPreviewer;
            }
        }


        private void Awake()
        {
            _instance = this;
            GetAnyComponents();
            SetPreviewMode(false, null);
        }


        public static void SetPreviewMode(bool state, AnywhenPlayerBase targetPlayer)
        {
            Instance._isPreviewing = state;
            if (state)
            {
                Instance.GetAnyComponents();
                targetPlayer.Play();
                Metronome.Play();
            }
            else
            {
                targetPlayer?.Stop();
            }
        }

        public void Init()
        {
            GetAnyComponents();
        }


        void GetAnyComponents()
        {
            TryGetComponent(out _metronome);
            TryGetComponent(out _conductor);
            TryGetComponent(out _anysongPlayerBrain);
            _instrumentDatabase = GetComponentInChildren<InstrumentDatabase>();
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
    }
}