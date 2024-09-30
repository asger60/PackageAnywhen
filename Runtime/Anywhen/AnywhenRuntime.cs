using UnityEditor;
using UnityEngine;

namespace Anywhen
{
    [RequireComponent(typeof(AnywhenMetronome))]
    [RequireComponent(typeof(AnywhenConductor))]
    [RequireComponent(typeof(AnywhenSamplePlayer))]
    [RequireComponent(typeof(AnywhenEventFunnel))]
    [RequireComponent(typeof(AnywhenNoteClipPreviewer))]

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class AnywhenRuntime : MonoBehaviour
    {
        private static AnywhenMetronome _metronome;
        public static AnywhenMetronome Metronome => _metronome;

        private static AnywhenConductor _conductor;
        public static AnywhenConductor Conductor => _conductor;

        private static AnywhenNoteClipPreviewer _previewer;

        static AnywhenEventFunnel _eventFunnel;
        public static AnywhenSamplePlayer AnywhenSamplerHandler => _anywhenSamplerHandler;

        private static AnywhenSamplePlayer _anywhenSamplerHandler;

        public static AnywhenSynthPlayer AnywhenSynthHandler => _anywhenSynthHandler;
        private static AnywhenSynthPlayer _anywhenSynthHandler;
        public static AnywhenEventFunnel EventFunnel => _eventFunnel;


        private static AnywhenRuntime _instance;
        private static bool _executeInEditMode;
        private static AnysongPlayer _targetPlayer;
        public static int SampleRate;


#if UNITY_EDITOR
        static AnywhenRuntime()
        {
            EditorApplication.update += EditorUpdate;
        }
#endif
        static void EditorUpdate()
        {
            if (Application.isPlaying) return;
            if (_executeInEditMode)
            {
                Metronome.Update();
                AnywhenSynthHandler.LateUpdate();
            }
        }

        public static AnywhenNoteClipPreviewer ClipPreviewer
        {
            get
            {
                if (_previewer != null)
                    return _previewer;
                _previewer = FindObjectOfType<AnywhenRuntime>().GetComponent<AnywhenNoteClipPreviewer>();
                return _previewer;
            }
        }


        private void Awake()
        {
            _instance = this;
            GetAnyComponents();
            SetPreviewMode(false, null);
            SampleRate = AudioSettings.outputSampleRate;
        }


        public static void SetPreviewMode(bool state, AnysongPlayer targetPlayer)
        {
            _instance = FindObjectsByType<AnywhenRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
            if (state)
            {
                _instance.GetAnyComponents();
                _anywhenSynthHandler.ClearPresets();
                _anywhenSynthHandler.Init();
                _anywhenSamplerHandler.Init();
                targetPlayer.Load();
                targetPlayer.Play();
                Metronome.Play();
            }
            else
            {
                _anywhenSynthHandler.ClearPresets();
            }

            _executeInEditMode = state;
        }

        public void Init()
        {
            GetAnyComponents();
            _anywhenSamplerHandler.CreateSamplers();
            _anywhenSynthHandler.CreateSynths();
        }

        void GetAnyComponents()
        {
            TryGetComponent(out _metronome);
            TryGetComponent(out _conductor);
            TryGetComponent(out _anywhenSamplerHandler);
            TryGetComponent(out _eventFunnel);
            TryGetComponent(out _anywhenSynthHandler);
        }
    }
}