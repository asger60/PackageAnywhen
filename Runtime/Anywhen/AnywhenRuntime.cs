#if UNITY_EDITOR
using UnityEditor;
#endif
using Anywhen.Composing;
using UnityEngine;

namespace Anywhen
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class AnywhenRuntime : MonoBehaviour
    {
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

        private static AnywhenNoteClipPreviewer _noteClipPreviewer;

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



        public static AnywhenSynthPlayer AnywhenSynthHandler => _anywhenSynthHandler;
        private static AnywhenSynthPlayer _anywhenSynthHandler;

        private static InstrumentDatabase _instrumentDatabase;

        public static InstrumentDatabase InstrumentDatabase
        {
            get
            {
                if (!_instrumentDatabase)
                    Instance.GetAnyComponents();

                return _instrumentDatabase;
            }
        }

        private static AnywhenRuntime _instance;

        private static AnywhenRuntime Instance
        {
            get
            {
                if (_instance == null)
                {
                    var anywhens =
                        FindObjectsByType<AnywhenRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (anywhens.Length == 0) return null;
                    _instance = anywhens[0];

                    _instance.GetAnyComponents();
                }

                return _instance;
            }
        }

        private static bool _executeInEditMode;
        private static AnywhenPlayer _targetPlayer;

        private static int _sampleRate;

        public static int SampleRate
        {
            get
            {
                if (_sampleRate == 0)
                    _sampleRate = 1;

                return _sampleRate;
            }
        }

        private bool _isPreviewing;
        [SerializeField] private bool logErrors;
        public static bool IsPreviewing => Instance._isPreviewing;

#if UNITY_EDITOR
        static AnywhenRuntime()
        {
            EditorApplication.update += EditorUpdate;
        }
#endif
        static void EditorUpdate()
        {
            if (Application.isPlaying) return;
            if (_sampleRate == 1)
                _sampleRate = AudioSettings.outputSampleRate;
            if (_executeInEditMode)
            {
                Metronome.Update();
                AnywhenSynthHandler.LateUpdate();
            }
        }

        public static AnywhenNoteClipPreviewer ClipNoteClipPreviewer
        {
            get
            {
                if (_noteClipPreviewer == null)
                    Instance.GetAnyComponents();
                return _noteClipPreviewer;
            }
        }


        private void Awake()
        {
            _instance = this;
            GetAnyComponents();
            SetPreviewMode(false, null);
        }


        public static void SetPreviewMode(bool state, AnywhenPlayer targetPlayer)
        {
            Instance._isPreviewing = state;
            if (state)
            {
                Instance.GetAnyComponents();
                _anywhenSynthHandler.ClearPresets();
                _anywhenSynthHandler.Init();

                targetPlayer.Play();
                Metronome.Play();
            }
            else
            {
                targetPlayer?.Stop();
                _anywhenSynthHandler.ClearPresets();
            }

            _executeInEditMode = state;
        }

        public void Init()
        {
            GetAnyComponents();
            _anywhenSynthHandler.CreateSynths();
        }

        void GetAnyComponents()
        {
            TryGetComponent(out _metronome);
            TryGetComponent(out _conductor);
            TryGetComponent(out _anysongPlayerBrain);
            TryGetComponent(out _noteClipPreviewer);
            _anywhenSynthHandler = GetComponentInChildren<AnywhenSynthPlayer>();
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
            if (!_instance.logErrors) return;
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
    }
}